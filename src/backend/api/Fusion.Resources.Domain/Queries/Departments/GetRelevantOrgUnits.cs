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
    public class GetRelevantOrgUnits : IRequest<QueryRangedList<QueryRelevantOrgUnit>>
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

        public class Handler : IRequestHandler<GetRelevantOrgUnits, QueryRangedList<QueryRelevantOrgUnit>>
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

            public async Task<List<QueryRelevantOrgUnit>> GetAllOrgUnitsAsync()
            {
                var orgUnits = await memCache.GetOrCreateAsync(CACHEKEY, async (entry) =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60);



                    var orgUnitResponse = await lineOrgClient.GetFromJsonAsync<ApiPagedCollection<ApiOrgUnit>>("/org-units?$top=10000");
                    if (orgUnitResponse is null)
                        throw new InvalidOperationException("Could not fetch org units from line org");

                    List<QueryRelevantOrgUnit> QueryorgUnits = orgUnitResponse.Value.Select(org => new QueryRelevantOrgUnit
                    {
                        FullDepartment = org.FullDepartment,
                        Name = org.Name,
                        SapId = org.SapId,
                        ParentSapId = org.Parent?.SapId,
                        ShortName = org.ShortName,
                        Department = org.Department,

                    }).ToList();

                    return QueryorgUnits;

                });



                return orgUnits;
            }

            private CancellationToken _cancellationToken;
            public async Task<QueryRangedList<QueryRelevantOrgUnit>> Handle(GetRelevantOrgUnits request, CancellationToken cancellationToken)
            {

                var cachedOrgUnits = await GetAllOrgUnitsAsync();

                _cancellationToken = cancellationToken;
                var user = await profileResolver.ResolvePersonFullProfileAsync(request.ProfileId.OriginalIdentifier);

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
                    delegatedParentManagerWithResposibility.AddRange(delegatedChildren.Children);
                }

                var adminClaims = user.Roles?.Where(x => x.Name.StartsWith("Fusion.Resources.Full") || x.Name.StartsWith("Fusion.Resources.Admin")).Select(x => x.Scope?.Value);
                var readClaims = user.Roles?.Where(x => x.Name.StartsWith("Fusion.Resources.Request") || x.Name.StartsWith("Fusion.Resources.Read")).Select(x => x.Scope?.Value);

                var orgUnitAccessReason = new List<QueryOrgUnitReason>();

                if (isDepartmentManager) orgUnitAccessReason.Add(new QueryOrgUnitReason
                {
                    FullDepartment = user.FullDepartment,
                    Reason = "Manager"
                });
                orgUnitAccessReason.AddRange(adminClaims.Select(dep => new QueryOrgUnitReason
                {
                    FullDepartment = dep ?? "*",
                    Reason = "Write"
                }));
                //retList.AddRange(readClaims.Select(dep => new QueryOrgUnit
                //{
                //    FullDepartment = dep,
                //    Reason = "Read"
                //}));
                orgUnitAccessReason.AddRange(departmentsWithResponsibility.Select(dep => new QueryOrgUnitReason
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
                if (isDepartmentManager) orgUnitAccessReason.AddRange(lineOrgDepartmentProfile?.Children.Select(dep => new QueryOrgUnitReason
                {
                    FullDepartment = dep.DepartmentId,
                    Reason = "ParentManager"
                }) ?? Array.Empty<QueryOrgUnitReason>());
                //retList.AddRange(lineOrgDepartmentProfile?.Siblings.Select(dep => new QueryOrgUnit
                //{
                //    FullDepartment = dep.DepartmentId,
                //    Reason = "RelevantSibling"
                //}) ?? Array.Empty<QueryOrgUnit>());
                orgUnitAccessReason.AddRange(delegatedParentManagerWithResposibility?.Select(dep => new QueryOrgUnitReason
                {
                    FullDepartment = dep.DepartmentId,
                    Reason = "DelegatedParentManager"
                }) ?? Array.Empty<QueryOrgUnitReason>());

                List<QueryRelevantOrgUnit> populatedOrgUnitResult = PopulateOrgUnits(cachedOrgUnits, orgUnitAccessReason);

                var filteredOrgUnits = ApplyOdataFilters(request.Query, populatedOrgUnitResult);

                var skip = request.Query.Skip.GetValueOrDefault(0);

                var pagedQuery = QueryRangedList.FromItems(filteredOrgUnits, filteredOrgUnits.Count, skip);

                return pagedQuery;
            }

            private static List<QueryRelevantOrgUnit> PopulateOrgUnits(List<QueryRelevantOrgUnit> cachedOrgUnits, List<QueryOrgUnitReason> orgUnitAccessReason)
            {
                var endResult = new List<QueryRelevantOrgUnit>();
                foreach (var org in orgUnitAccessReason)
                {
                    if (org?.FullDepartment != null)
                    {
                        var alreadyInList = endResult.FirstOrDefault(x => x.FullDepartment == org.FullDepartment);

                        if (alreadyInList is null)
                        {
                            var data = cachedOrgUnits.Where(x => x.FullDepartment == org?.FullDepartment.Replace('*', ' ').TrimEnd()).FirstOrDefault();

                            if (data != null)
                            {
                                if (!data.Reasons.Contains(org.Reason))
                                {
                                    data.Reasons.Add(org.Reason);
                                }

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

                return endResult;
            }

            private static List<QueryRelevantOrgUnit> ApplyOdataFilters(ODataQueryParams filter, List<QueryRelevantOrgUnit> orgUnits)
            {

                var filterGenerator = filter.GenerateFilters<QueryRelevantOrgUnit>(m =>
                {
                    m.SqlQueryMode = false;
                    m.MapField("sapId", e => e.SapId);
                    m.MapField("name", e => e.Name);
                    m.MapField("shortName", e => e.ShortName);
                    m.MapField("department", e => e.Department);
                    m.MapField("fullDepartment", e => e.FullDepartment);
                    m.MapField("reason", e => e.Reasons);
                });
                return orgUnits.Where(filterGenerator.FilterLambda.Compile()).ToList();
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