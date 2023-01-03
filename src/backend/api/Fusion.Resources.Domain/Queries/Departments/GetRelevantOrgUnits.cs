using Fusion.AspNetCore.OData;
using Fusion.Integration;
using Fusion.Integration.LineOrg;
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

using System.Threading;
using System.Threading.Tasks;


namespace Fusion.Resources.Domain.Queries
{
    /// <summary>
    /// Fetch the profile for a resource owner. Will compile a list of departments the person has responsibilities in and which is relevant.
    /// </summary>
    public class GetRelevantOrgUnits : IRequest<IEnumerable<QueryOrgUnit?>>
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

        public class Handler : IRequestHandler<GetRelevantOrgUnits, IEnumerable<QueryOrgUnit>>
        {
            private readonly TimeSpan defaultAbsoluteCacheExpirationHours = TimeSpan.FromHours(1);
            private readonly ILogger<Handler> logger;
            private readonly IFusionProfileResolver profileResolver;
            private readonly IFusionRolesClient rolesClient;
            private readonly IMediator mediator;
            private readonly ILineOrgResolver lineOrgResolver;
            private readonly IMemoryCache memCache;


      

            public Handler(ILogger<Handler> logger, IFusionProfileResolver profileResolver, IFusionRolesClient rolesClient, ILineOrgResolver lineOrgResolver, IMediator mediator, IMemoryCache memCache)
            {
                this.logger = logger;
                this.profileResolver = profileResolver;
                this.rolesClient = rolesClient;
                this.mediator = mediator;
                this.lineOrgResolver = lineOrgResolver;
                this.memCache = memCache;



            }
            private CancellationToken _cancellationToken;
            public async Task<IEnumerable<QueryOrgUnit>> Handle(GetRelevantOrgUnits request, CancellationToken cancellationToken)
            {
                _cancellationToken = cancellationToken;
                var user = await profileResolver.ResolvePersonFullProfileAsync(request.ProfileId.OriginalIdentifier);

                if (user?.FullDepartment is null) return new List<QueryOrgUnit>();

                // Resolve departments with responsibility
                var sector = await ResolveSector(user.FullDepartment);
                var departmentsWithResponsibility = await ResolveDepartmentsWithAccessAsync(user);
                var isDepartmentManager = departmentsWithResponsibility.Any(r => r.Value == user.FullDepartment);

                var relevantSectors = await ResolveRelevantSectorsAsync(user.FullDepartment, sector, isDepartmentManager, departmentsWithResponsibility);
                var relevantDepartments = new List<QueryDepartment>();

                foreach (var relevantSector in relevantSectors) relevantDepartments.AddRange(await ResolveSectorDepartments(relevantSector));

                var lineOrgDepartmentProfile = await mediator.Send(new GetRelatedDepartments(user.FullDepartment), cancellationToken);

                var wildcardDeparmtent = departmentsWithResponsibility.Where(x => x.Key.Contains('*'));
                var ParentmanagerWithResposibility = new List<QueryDepartment>();
                foreach (var wildcard in wildcardDeparmtent)
                {
                    //var result = await mediator.Send(new GetRelatedDepartments(wildcard.Key.Replace('*', ' ').TrimEnd()), cancellationToken);

                    var orgUnits = await memCache.GetOrCreateAsync(CACHEKEY, async (entry) =>
                    {
                        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60);



                        var orgUnitResponse = await mediator.Send(new GetRelatedDepartments(wildcard.Key.Replace('*', ' ').TrimEnd()), cancellationToken);
                        if (orgUnitResponse is null)
                            throw new InvalidOperationException("Could not fetch org units from line org");



                        return orgUnitResponse;
                    });


                    ParentmanagerWithResposibility.AddRange(orgUnits.Children);
                }

               


                var adminClaims = user.Roles.Where(x => x.Name.StartsWith("Fusion.Resources.Full")).Select(x => x.Scope?.Value).Where(y => y != null);
                var readClaims = user.Roles.Where(x => x.Name.StartsWith("Fusion.Resources.Request")).Select(x => x.Scope?.Value);


                var retList = new List<QueryOrgUnit>();


                if (isDepartmentManager) retList.Add(new QueryOrgUnit
                {
                    FullDepartment = user.FullDepartment,
                    Reason = "ResourceOwner"
                });
                retList.AddRange(adminClaims.Select(dep => new QueryOrgUnit
                {
                    FullDepartment = dep,
                    Reason = "Write"
                }));
                retList.AddRange(readClaims.Select(dep => new QueryOrgUnit
                {
                    FullDepartment = dep,
                    Reason = "Read"
                }));
                retList.AddRange(departmentsWithResponsibility.Select(dep => new QueryOrgUnit
                {
                    FullDepartment = dep.Key,
                    Reason = "DelegatedManager"
                }));
                retList.AddRange(relevantSectors.Select(dep => new QueryOrgUnit
                {
                    FullDepartment = dep,
                    Reason = "RelevantSector"
                }));
                retList.AddRange(relevantDepartments.Select(dep => new QueryOrgUnit
                {
                    FullDepartment = dep.DepartmentId,
                    Reason = "RelevantDepartment"
                }));
                retList.AddRange(lineOrgDepartmentProfile?.Children.Select(dep => new QueryOrgUnit
                {
                    FullDepartment = dep.DepartmentId,
                    Reason = "RelevantChild"
                }) ?? Array.Empty<QueryOrgUnit>());
                retList.AddRange(lineOrgDepartmentProfile?.Siblings.Select(dep => new QueryOrgUnit
                {
                    FullDepartment = dep.DepartmentId,
                    Reason = "RelevantSibling"
                }) ?? Array.Empty<QueryOrgUnit>());
                retList.AddRange(ParentmanagerWithResposibility?.Select(dep => new QueryOrgUnit
                {
                    FullDepartment = dep.DepartmentId,
                    Reason = "ParentManager"
                }) ?? Array.Empty<QueryOrgUnit>());



                foreach (var org in retList)
                {
                    if (org?.FullDepartment != null)
                    {
                        var data = await lineOrgResolver.ResolveOrgUnitAsync(DepartmentId.FromFullPath(org?.FullDepartment));
                        if (data != null)
                        {
                            org.sapId = data.SapId;
                            org.name = data.Name;
                            org.shortName = data.ShortName;
                            org.parentSapId = data.Parent.SapId;
                            org.department = data.Department;
                        }
                    }


                }


                return retList;
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