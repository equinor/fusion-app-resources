using Fusion.AspNetCore.OData;
using Fusion.Integration;
using Fusion.Integration.Profile;
using Fusion.Integration.Roles;
using Fusion.Resources.Domain.Models;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
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

        public class Handler : IRequestHandler<GetRelevantOrgUnits, QueryRangedList<QueryRelevantOrgUnit>?>
        {

            private readonly IFusionProfileResolver profileResolver;
            private readonly IFusionRolesClient rolesClient;
            private readonly IMediator mediator;
            private readonly IMemoryCache memCache;
            private readonly IOrgUnitCache orgUnitCache;
            private CancellationToken _cancellationToken;


            public Handler(IFusionProfileResolver profileResolver, IFusionRolesClient rolesClient, IMediator mediator, IMemoryCache memCache, IOrgUnitCache orgUnitCache)
            {
                this.profileResolver = profileResolver;
                this.rolesClient = rolesClient;
                this.mediator = mediator;
                this.memCache = memCache;
                this.orgUnitCache = orgUnitCache;
            }


            public async Task<QueryRangedList<QueryRelevantOrgUnit>?> Handle(GetRelevantOrgUnits request, CancellationToken cancellationToken)
            {
                var cachedOrgUnits = await orgUnitCache.GetOrgUnitsAsync();
                var orgUnits = cachedOrgUnits.Select(x => new QueryRelevantOrgUnit
                {
                    SapId = x.SapId,
                    Name = x.Name,
                    FullDepartment = x.FullDepartment,
                    Department = x.Department,
                    ShortName = x.ShortName
                });

                _cancellationToken = cancellationToken;
                var user = await profileResolver.ResolvePersonFullProfileAsync(request.ProfileId.OriginalIdentifier);

                if (user?.FullDepartment is null || user is null)
                {
                    return null;
                }

                // Resolve departments with responsibility
                var sector = await ResolveSector(user.FullDepartment);
                var departmentsWithResponsibility = await ResolveDepartmentsWithAccessAsync(user);
                var isDepartmentManager = departmentsWithResponsibility.Any(r => r == user.FullDepartment);

                var relevantSectors = await ResolveRelevantSectorsAsync(user.FullDepartment, sector, isDepartmentManager, departmentsWithResponsibility);
                var relevantDepartments = new List<QueryDepartment>();

                foreach (var relevantSector in relevantSectors) relevantDepartments.AddRange(await ResolveSectorDepartments(relevantSector));
                var lineOrgDepartmentProfile = new QueryRelatedDepartments();
                if (user?.FullDepartment is not null)
                { 
                    lineOrgDepartmentProfile = await ResolveCache(user.FullDepartment.Replace('*', ' ').TrimEnd(), cancellationToken);
                }

                var delegatedParentDeparmtent = departmentsWithResponsibility.Where(x => x.Contains('*'));
                var delegatedParentManagerWithResposibility = new List<QueryDepartment>();

                foreach (var wildcard in delegatedParentDeparmtent)
                {
                    // Needs to have cache here, or else it takses over 10 secodns to load.
                    var delegatedChildren = await ResolveCache(wildcard.Replace('*', ' ').TrimEnd(), cancellationToken);
                    if (delegatedChildren?.Children is not null)
                    {
                        delegatedParentManagerWithResposibility.AddRange(delegatedChildren.Children);
                    }
                }

                var adminClaims = user?.Roles?.Where(x => x.Name.StartsWith("Fusion.Resources.Full") || x.Name.StartsWith("Fusion.Resources.Admin")).Select(x => x.Scope?.Value);
                var readClaims = user?.Roles?.Where(x => x.Name.StartsWith("Fusion.Resources.Request") || x.Name.StartsWith("Fusion.Resources.Read")).Select(x => x.Scope?.Value);

                var orgUnitAccessReason = new List<QueryOrgUnitReason>();

                if (isDepartmentManager && user?.FullDepartment is not null) orgUnitAccessReason.Add(new QueryOrgUnitReason
                {
                    FullDepartment = user.FullDepartment,
                    Reason = ReasonRoles.Roles.Manager.ToString()
                });
                if (adminClaims is not null)
                {
                    orgUnitAccessReason.AddRange(adminClaims.Select(dep => new QueryOrgUnitReason
                    {
                        FullDepartment = dep ?? "*",
                        Reason = ReasonRoles.Roles.Write.ToString()
                    })); ;
                }
                orgUnitAccessReason.AddRange(departmentsWithResponsibility.Select(dep => new QueryOrgUnitReason
                {
                    FullDepartment = dep,
                    Reason = ReasonRoles.Roles.DelegatedManager.ToString()
                })); ;
                if (isDepartmentManager) orgUnitAccessReason.AddRange(lineOrgDepartmentProfile?.Children.Select(dep => new QueryOrgUnitReason
                {
                    FullDepartment = dep.DepartmentId,
                    Reason = ReasonRoles.Roles.ParentManager.ToString()
                }) ?? Array.Empty<QueryOrgUnitReason>()); ;

                orgUnitAccessReason.AddRange(delegatedParentManagerWithResposibility?.Select(dep => new QueryOrgUnitReason
                {
                    FullDepartment = dep.DepartmentId,
                    Reason = ReasonRoles.Roles.DelegatedParentManager.ToString()
                }) ?? Array.Empty<QueryOrgUnitReason>()); ;

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

                //retList.AddRange(lineOrgDepartmentProfile?.Siblings.Select(dep => new QueryOrgUnit
                //{
                //    FullDepartment = dep.DepartmentId,
                //    Reason = "RelevantSibling"
                //}) ?? Array.Empty<QueryOrgUnit>());

                //retList.AddRange(readClaims.Select(dep => new QueryOrgUnit
                //{
                //    FullDepartment = dep,
                //    Reason = "Read"
                //}));

                List<QueryRelevantOrgUnit> populatedOrgUnitResult = GetRelevantOrgUnits(orgUnits, orgUnitAccessReason);

                var filteredOrgUnits = ApplyOdataFilters(request.Query, populatedOrgUnitResult.OrderBy(x => x.SapId));

                var skip = request.Query.Skip.GetValueOrDefault(0);
                var take = request.Query.Top.GetValueOrDefault(100);

                filteredOrgUnits.DistinctBy(x => x.SapId);

                var pagedQuery = QueryRangedList.FromEnumerableItems(filteredOrgUnits, skip, take);

                return pagedQuery;
            }

            private static List<QueryRelevantOrgUnit> GetRelevantOrgUnits(IEnumerable<QueryRelevantOrgUnit> cachedOrgUnits, List<QueryOrgUnitReason> orgUnitAccessReason)
            {
                var endResult = new List<QueryRelevantOrgUnit>();
                foreach (var org in orgUnitAccessReason)
                {

                    if (org?.FullDepartment != null && org?.Reason != null)
                    {
                        var alreadyInList = endResult.FirstOrDefault(x => x.FullDepartment == org.FullDepartment);

                        if (alreadyInList is null)
                        {
                            QueryRelevantOrgUnit? data = new QueryRelevantOrgUnit();
                            if (org.IsWildCard == true)
                            {
                                data = cachedOrgUnits.FirstOrDefault(x => x.FullDepartment == org.FullDepartment.Replace('*', ' ').TrimEnd());
                            }
                            else
                            {
                                data = cachedOrgUnits.FirstOrDefault(x => x.FullDepartment == org.FullDepartment);
                            }

                            if (data != null)
                            {
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

                return endResult;
            }

            private static List<QueryRelevantOrgUnit> ApplyOdataFilters(ODataQueryParams filter, IEnumerable<QueryRelevantOrgUnit> orgUnits)
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

            private async Task<QueryRelatedDepartments?> ResolveCache(string fullDepartmentName, CancellationToken cancellationToken)
            {
                return await memCache.GetOrCreateAsync(fullDepartmentName.Trim(), async (entry) =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60);

                    var orgUnitResponse = await mediator.Send(new GetRelatedDepartments(fullDepartmentName), cancellationToken);
                    //if (orgUnitResponse is null)
                        //throw new InvalidOperationException("Could not fetch org units from line org");
                    return orgUnitResponse;
                });
            }

            private async Task<List<string>> ResolveRelevantSectorsAsync(string? fullDepartment, string? sector, bool isDepartmentManager, List<string> departmentsWithResponsibility)
            {
                // Get sectors the user have responsibility in, to find all relevant departments
                var relevantSectors = new List<string>();
                foreach (var department in departmentsWithResponsibility)
                {

                    var resolvedSector = await ResolveSector(department.ToString());
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

            private async Task<List<string>> ResolveDepartmentsWithAccessAsync(FusionPersonProfile user)
            {

                var departmentsWithResponsibility = new List<string>();
                // Add all departments the user has been delegated responsibility for.

                var roleAssignedDepartments = await rolesClient.GetRolesAsync(q => q
                    .WherePersonAzureId(user.AzureUniqueId!.Value)
                );

                departmentsWithResponsibility.AddRange(roleAssignedDepartments
                    .Where(x => x.Scope != null && x.ValidTo >= DateTimeOffset.Now)
                    .SelectMany(x => x.Scope!.Values)
                );

                var isDepartmentManager = user.IsResourceOwner;
                // Add the current department if the user is resource owner in the department.
                if (isDepartmentManager && user.FullDepartment != null)
                    departmentsWithResponsibility.Add(user.FullDepartment);

                return departmentsWithResponsibility;
            }

        }
    }
}