using AdaptiveCards;
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

                var user = await profileResolver.ResolvePersonFullProfileAsync(request.ProfileId.OriginalIdentifier);

                if (user?.FullDepartment is null)
                {
                    return null;
                }

                // Resolve claims with responsibility.
                var delegatedDepartmentManagerClaims = user.Roles?.Where(x => x.Name.StartsWith("Fusion.Resources.ResourceOwner")).Select(x => x.Scope?.Value);
                var adminClaims = user.Roles?.Where(x => x.Name.StartsWith("Fusion.Resources.Full") && x.IsActive == true || x.Name.StartsWith("Fusion.Resources.Admin") && x.IsActive == true).Select(x => x.Scope?.Value);
                var readClaims = user.Roles?.Where(x => x.Name.StartsWith("Fusion.Resources.Request") && x.IsActive == true || x.Name.StartsWith("Fusion.Resources.Read") && x.IsActive == true).Select(x => x.Scope?.Value);

                var orgUnitAccessReason = new List<QueryOrgUnitReason>();

                orgUnitAccessReason.applyManager(user);
                orgUnitAccessReason.applyRole(adminClaims, ReasonRoles.Write);
                orgUnitAccessReason.applyRole(delegatedDepartmentManagerClaims, ReasonRoles.DelegatedManager);
                orgUnitAccessReason.applyRole(readClaims, ReasonRoles.Read);
                orgUnitAccessReason.applyParentManager(orgUnits, user);

                if (orgUnitAccessReason is null)
                {
                    return null;
                }

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
                    return orgUnitResponse;
                });
            }
        }
    }

    internal static class orgUnitAccessReasons
    {
        internal static void applyManager(this List<QueryOrgUnitReason> reasons, FusionFullPersonProfile user)
        {
            var isDepartmentManager = user.IsResourceOwner;
            if (isDepartmentManager) reasons.Add(new QueryOrgUnitReason
            {
                FullDepartment = user?.FullDepartment ?? "",
                Reason = ReasonRoles.Manager
            });
        }

        internal static void applyRole(this List<QueryOrgUnitReason> reasons, IEnumerable<string?>? departments, string role)
        {
            if (departments is not null)
            {
                reasons.AddRange(departments.Select(dep => new QueryOrgUnitReason
                {
                    FullDepartment = dep ?? "*",
                    Reason = role
                }));
            }
        }

        internal static void applyParentManager(this List<QueryOrgUnitReason> reasons, IEnumerable<QueryRelevantOrgUnit> orgUnits, FusionFullPersonProfile user)
        {
            var delegatedParentManagerWithResposibility = new List<QueryOrgUnitReason>();
            var delegatedParentManagerClaim = reasons?.Where(x => x.IsWildCard == true); ;
            if (delegatedParentManagerClaim is not null)
            {
                foreach (var wildcard in delegatedParentManagerClaim)
                {
                    var wildcardDepartment = wildcard.FullDepartment.Replace("*", "").TrimEnd();
                    var delegatedChildren = orgUnits.Distinct().Where(x => x.FullDepartment.StartsWith(wildcardDepartment));
                    var reason = ReasonRoles.DelegatedParentManager;
                    if (user.IsResourceOwner && user.FullDepartment == wildcardDepartment)
                    {
                        reason = ReasonRoles.ParentManager;
                    }

                    foreach (var child in delegatedChildren)
                    {
                        delegatedParentManagerWithResposibility?.Add(new QueryOrgUnitReason
                        {
                            FullDepartment = child.FullDepartment,
                            Reason = reason
                        });
                    }
                }
                reasons?.AddRange(delegatedParentManagerWithResposibility);
            }
        }
    }
}