using Fusion.AspNetCore.OData;
using Fusion.Integration;
using Fusion.Integration.LineOrg;
using Fusion.Integration.LineOrg.Cache;
using Fusion.Integration.Profile;
using Fusion.Integration.Roles;
using Fusion.Resources.Domain.Models;
using Fusion.Services.LineOrg.ApiModels;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;


namespace Fusion.Resources.Domain.Queries
{
    /// <summary>
    /// Fetch the profile for a resource owner. Will compile a list of departments the person has responsibilities in and which is relevant.
    /// </summary>
    public class GetRelevantOrgUnits : IRequest<IEnumerable<QueryRelevantOrgUnit>>
    {
        public GetRelevantOrgUnits(string profileId, AspNetCore.OData.ODataQueryParams query)
        {
            ProfileId = profileId;
            Query = query;
        }

        /// <summary>
        /// Mail or azure unique id
        /// </summary>
        public PersonId ProfileId { get; set; }
        public ODataQueryParams Query { get; }
        public const string CACHEKEY = "Cache.OrgUnits";
        public enum Roles
        {
            DelegatedManager,
            DelegatedParentManager,
            ParentManager,
            Manager,
            Write
        }

        public class Handler : IRequestHandler<GetRelevantOrgUnits, IEnumerable<QueryRelevantOrgUnit>>
        {
            private readonly TimeSpan defaultAbsoluteCacheExpirationHours = TimeSpan.FromHours(1);
            private readonly ILogger<Handler> logger;
            private readonly IFusionProfileResolver profileResolver;
            private readonly IFusionRolesClient rolesClient;
            private readonly IMediator mediator;
            private readonly ILineOrgResolver lineOrgResolver;
            private readonly IMemoryCache memCache;
            private readonly HttpClient lineOrgClient;




            public Handler(ILogger<Handler> logger, IFusionProfileResolver profileResolver, IFusionRolesClient rolesClient, ILineOrgResolver lineOrgResolver, IMediator mediator, IMemoryCache memCache, IHttpClientFactory httpClientFactory)
            {
                this.logger = logger;
                this.profileResolver = profileResolver;
                this.rolesClient = rolesClient;
                this.mediator = mediator;
                this.lineOrgResolver = lineOrgResolver;
                this.memCache = memCache;
                this.lineOrgClient = httpClientFactory.CreateClient(IntegrationConfig.HttpClients.ApplicationLineOrg());





            }

            public async Task<List<QueryRelevantOrgUnit>> ResolveAllOrgUnits()
            {
                var orgUnits = await memCache.GetOrCreateAsync(CACHEKEY, async (entry) =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60);



                    var orgUnitResponse = await lineOrgClient.GetFromJsonAsync<ApiPagedCollection<ApiOrgUnit>>("/org-units?$top=10000");
                    if (orgUnitResponse is null)
                        throw new InvalidOperationException("Could not fetch org units from line org");

                    List<QueryRelevantOrgUnit> QueryorgUnits = orgUnitResponse.Value.Select(org => new QueryRelevantOrgUnit
                    {
                        FullDepartment = org.FullDepartment ?? "",
                        Name = org.Name ?? "",
                        SapId = org.SapId ?? "",
                        ParentSapId = org.Parent?.SapId ?? "",
                        ShortName = org.ShortName ?? "",
                        Department = org.Department ?? "",

                    }).ToList();

                    return QueryorgUnits;

                });

            

                return orgUnits;
            }

            private CancellationToken _cancellationToken;
            public async Task<IEnumerable<QueryRelevantOrgUnit>> Handle(GetRelevantOrgUnits request, CancellationToken cancellationToken)
            {

                var cachedOrgUnits = await ResolveAllOrgUnits();

                _cancellationToken = cancellationToken;
                var user = await profileResolver.ResolvePersonFullProfileAsync(request.ProfileId.OriginalIdentifier);

                if (user?.FullDepartment is null) return new List<QueryRelevantOrgUnit>();

                // Resolve departments with responsibility
                var sector = await ResolveSector(user.FullDepartment);
                var departmentsWithResponsibility = await ResolveDepartmentsWithAccessAsync(user);
                var isDepartmentManager = departmentsWithResponsibility.Any(r => r.Value == user.FullDepartment);

                var relevantSectors = await ResolveRelevantSectorsAsync(user.FullDepartment, sector, isDepartmentManager, departmentsWithResponsibility);
                var relevantDepartments = new List<QueryDepartment>();

                foreach (var relevantSector in relevantSectors) relevantDepartments.AddRange(await ResolveSectorDepartments(relevantSector));



                var lineOrgDepartmentProfile = await ResolveCache(user.FullDepartment.Replace('*', ' ').TrimEnd(), "Cache.lineOrgDepartmentProfile", cancellationToken);



                var delegatedParentDeparmtent = departmentsWithResponsibility.Where(x => x.Key.Contains('*'));
                var delegatedParentManagerWithResposibility = new List<QueryDepartment>();
                foreach (var wildcard in delegatedParentDeparmtent)
                {
                    var delegatedChildren = await ResolveCache(wildcard.Key.Replace('*', ' ').TrimEnd(), "Cache.wildcardChildren", cancellationToken);
                    //var orgUnits = await mediator.Send(new GetRelatedDepartments(wildcard.Key.Replace('*', ' ').TrimEnd()), cancellationToken);

                    delegatedParentManagerWithResposibility.AddRange(delegatedChildren.Children);
                }




                var adminClaims = user.Roles?.Where(x => x.Name.StartsWith("Fusion.Resources.Full") || x.Name.StartsWith("Fusion.Resources.Admin")).Select(x => x.Scope?.Value);
                var readClaims = user.Roles?.Where(x => x.Name.StartsWith("Fusion.Resources.Request") || x.Name.StartsWith("Fusion.Resources.Read")).Select(x => x.Scope?.Value);


                var retList = new List<QueryOrgUnitReason>();


                if (isDepartmentManager) retList.Add(new QueryOrgUnitReason
                {
                    FullDepartment = user.FullDepartment,
                    Reason = "Manager"
                });
                retList.AddRange(adminClaims.Select(dep => new QueryOrgUnitReason
                {
                    FullDepartment = dep ?? "*",
                    Reason = "Write"
                }));
                //retList.AddRange(readClaims.Select(dep => new QueryOrgUnit
                //{
                //    FullDepartment = dep,
                //    Reason = "Read"
                //}));
                retList.AddRange(departmentsWithResponsibility.Select(dep => new QueryOrgUnitReason
                {
                    FullDepartment = dep.Key,
                    Reason = "DelegatedManager"
                }));
                //retList.AddRange(relevantSectors.Select(dep => new QueryOrgUnit
                //{
                //    FullDepartment = dep,
                //    Reason = "RelevantSector"
                //}));
                //retList.AddRange(relevantDepartments.Select(dep => new QueryOrgUnit
                //{
                //    FullDepartment = dep.DepartmentId,
                //    Reason = "RelevantDepartment"
                //}));
                if (isDepartmentManager) retList.AddRange(lineOrgDepartmentProfile?.Children.Select(dep => new QueryOrgUnitReason
                {
                    FullDepartment = dep.DepartmentId,
                    Reason = "ParentManager"
                }) ?? Array.Empty<QueryOrgUnitReason>());
                //retList.AddRange(lineOrgDepartmentProfile?.Siblings.Select(dep => new QueryOrgUnit
                //{
                //    FullDepartment = dep.DepartmentId,
                //    Reason = "RelevantSibling"
                //}) ?? Array.Empty<QueryOrgUnit>());
                retList.AddRange(delegatedParentManagerWithResposibility?.Select(dep => new QueryOrgUnitReason
                {
                    FullDepartment = dep.DepartmentId,
                    Reason = "DelegatedParentManager"
                }) ?? Array.Empty<QueryOrgUnitReason>());


                var endResult = new List<QueryRelevantOrgUnit>();
                foreach (var org in retList)
                {
                    if (org?.FullDepartment != null)
                    {
                        var alreadyInList = endResult.FirstOrDefault(x => x.FullDepartment == org.FullDepartment);

                        if (alreadyInList is null)
                        {
                            var data = cachedOrgUnits.Where(x => x.FullDepartment == org?.FullDepartment.Replace('*', ' ').TrimEnd()).FirstOrDefault();




                            if (data != null)
                            {
                                data.Reasons.Clear();
                                data.Reasons.Add(org.Reason);
                                endResult.Add(data);
                            }

                        }
                        else
                        {
                            if (!alreadyInList.Reasons.Contains(org.Reason))
                            {

                                alreadyInList.Reasons.Add(org.Reason);
                            }

                        }




                    }


                }

                return ApplyOdataFilters(request.Query.Filter, endResult);
            }

            private static List<QueryRelevantOrgUnit> ApplyOdataFilters(ODataExpression filter, List<QueryRelevantOrgUnit> orgUnits)
            {
                var sapIdFilter = filter.GetFilterForField("sapId");
                if (sapIdFilter != null)
                {
                    if (sapIdFilter.Operation != FilterOperation.Eq)
                        throw new ArgumentException("Only the 'eq' operator is supported for field 'sapId'.");

                    orgUnits = orgUnits.Where(x => x.SapId == sapIdFilter.Value).ToList();
                }

                var nameFilter = filter.GetFilterForField("name");
                if (nameFilter != null)
                {
                    if (nameFilter.Operation == FilterOperation.Eq)
                    {
                        orgUnits = orgUnits.Where(x => x.Name == nameFilter.Value).ToList();
                    }

                    else if (nameFilter.Operation == FilterOperation.Contains)
                    {
                        orgUnits = orgUnits.Where(x => x.Name.Contains(nameFilter.Value)).ToList();
                    }

                    else if (nameFilter.Operation == FilterOperation.StartsWith)
                    {
                        orgUnits = orgUnits.Where(x => x.Name.StartsWith(nameFilter.Value)).ToList();
                    }

                    else if (nameFilter.Operation == FilterOperation.EndsWith)
                    {
                        orgUnits = orgUnits.Where(x => x.Name.EndsWith(nameFilter.Value)).ToList();
                    }
                    else
                    {
                        throw new ArgumentException($"The '{nameFilter.Operation}' operator is NOT supported for field 'name'.");
                    }
                }

                var departmentFilter = filter.GetFilterForField("department");
                if (departmentFilter != null)
                {
                    if (departmentFilter.Operation == FilterOperation.Eq)
                    {
                        orgUnits = orgUnits.Where(x => x.Department == departmentFilter.Value).ToList();
                    }

                    else if (departmentFilter.Operation == FilterOperation.Contains)
                    {
                        orgUnits = orgUnits.Where(x => x.Department.Contains(departmentFilter.Value)).ToList();
                    }

                    else if (departmentFilter.Operation == FilterOperation.StartsWith)
                    {
                        orgUnits = orgUnits.Where(x => x.Department.StartsWith(departmentFilter.Value)).ToList();
                    }

                    else if (departmentFilter.Operation == FilterOperation.EndsWith)
                    {
                        orgUnits = orgUnits.Where(x => x.Department.EndsWith(departmentFilter.Value)).ToList();
                    }
                    else
                    {
                        throw new ArgumentException(
                            $"The '{departmentFilter.Operation}' operator is NOT supported for field 'department'.");
                    }
                }

                var fullDepartmentFilter = filter.GetFilterForField("fulldepartment");
                if (fullDepartmentFilter != null)
                {
                    if (fullDepartmentFilter.Operation == FilterOperation.Eq)
                    {
                        orgUnits = orgUnits.Where(x => x.FullDepartment == fullDepartmentFilter.Value).ToList();
                    }

                    else if (fullDepartmentFilter.Operation == FilterOperation.Contains)
                    {
                        orgUnits = orgUnits.Where(x => x.FullDepartment.Contains(fullDepartmentFilter.Value)).ToList();
                    }

                    else if (fullDepartmentFilter.Operation == FilterOperation.StartsWith)
                    {
                        orgUnits = orgUnits.Where(x => x.FullDepartment.StartsWith(fullDepartmentFilter.Value)).ToList();
                    }

                    else if (fullDepartmentFilter.Operation == FilterOperation.EndsWith)
                    {
                        orgUnits = orgUnits.Where(x => x.FullDepartment.EndsWith(fullDepartmentFilter.Value)).ToList();
                    }
                    else
                    {
                        throw new ArgumentException(
                            $"The '{fullDepartmentFilter.Operation}' operator is NOT supported for field 'fullDepartment'.");
                    }
                }

                var reasonFilter = filter.GetFilterForField("reason");
                if (reasonFilter != null)
                {
                    if (reasonFilter.Operation == FilterOperation.Eq)
                    {
                        orgUnits = orgUnits.Where(x => x.Reasons.Contains(reasonFilter.Value)).ToList();
                    }

                    else if (reasonFilter.Operation == FilterOperation.Contains)
                    {
                        orgUnits = orgUnits.Where(x => x.Reasons.Contains(reasonFilter.Value)).ToList();
                    }
                    else
                    {
                        throw new ArgumentException(
                            $"The '{reasonFilter.Operation}' operator is NOT supported for field 'reason'.");
                    }
                }

                return orgUnits;
            }
            private async Task<QueryRelatedDepartments> ResolveCache(string fullDepartmentName, string cachekey, CancellationToken cancellationToken)
            {
                return await memCache.GetOrCreateAsync(cachekey, async (entry) =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60);



                    var orgUnitResponse = await mediator.Send(new GetRelatedDepartments(fullDepartmentName), cancellationToken);
                    if (orgUnitResponse is null)
                        throw new InvalidOperationException("Could not fetch org units from line org");



                    return orgUnitResponse;
                });
            }

            private async Task<List<string>> ResolveRelevantSectorsAsync(string? fullDepartment, string? sector, bool isDepartmentManager, Dictionary<string, string> departmentsWithResponsibility)
            {
                // Get sectors the user have responsibility in, to find all relevant departments
                var relevantSectors = new List<string>();
                foreach (var department in departmentsWithResponsibility)
                {

                    var resolvedSector = await ResolveSector(department.Key.ToString());
                    if (resolvedSector != null)
                    {
                        relevantSectors.Add(resolvedSector);
                    }
                }
                //If the sector does not exist, the person might be higher up.
                if (sector is null && isDepartmentManager)
                {

                    var downstreamSectors = await ResolveDownstreamSectors(fullDepartment);
                    foreach (var department in downstreamSectors)
                    {
                        var resolvedSector = await ResolveSector(department.DepartmentId);
                        if (resolvedSector != null)
                        {
                            relevantSectors.Add(resolvedSector);
                        }
                    }
                }
                return relevantSectors.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            }

            private async Task<string?> ResolveSector(string? department)
            {
                if (string.IsNullOrEmpty(department))
                    return null;
                var request = new GetDepartmentSector(department);
                return await mediator.Send(request);
            }

            private async Task<IEnumerable<QueryDepartment>> ResolveSectorDepartments(string sector)
            {
                var departments = await mediator.Send(new GetDepartments().InSector(sector));
                return departments.DistinctBy(dpt => dpt.DepartmentId);
            }

            private async Task<IEnumerable<QueryDepartment>> ResolveDownstreamSectors(string? department)
            {
                if (department is null)
                    return Array.Empty<QueryDepartment>();

                var departments = await mediator.Send(new GetDepartments().StartsWith(department));
                return departments.DistinctBy(x => x.DepartmentId);
            }





            private async Task<Dictionary<string, string>> ResolveDepartmentsWithAccessAsync(FusionPersonProfile user)
            {
                var isDepartmentManager = user.IsResourceOwner;

                var departmentsWithResponsibility = new Dictionary<string, string>();

                // Add the current department if the user is resource owner in the department.
                if (isDepartmentManager && user.FullDepartment != null)
                    departmentsWithResponsibility.Add(user.FullDepartment, "Fusion.Resources.ResourceOwner");

                // Add all departments the user has Access To.

                var roleAssignedDepartments = await rolesClient.GetRolesAsync(q => q
                    .WherePersonAzureId(user.AzureUniqueId!.Value)

                );


                foreach (var role in roleAssignedDepartments)
                {

                    if (role.ValidTo >= DateTimeOffset.Now && role.Scope!.Values != null)
                    {
                        departmentsWithResponsibility.Add(role.Scope.Values?.FirstOrDefault(), role.RoleName);
                    }
                }

                return departmentsWithResponsibility;
            }



        }
    }
}