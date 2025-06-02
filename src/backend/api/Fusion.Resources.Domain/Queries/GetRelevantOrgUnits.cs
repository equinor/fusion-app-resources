using Azure.Core;
using Fusion.AspNetCore.OData;
using Fusion.Integration;
using Fusion.Integration.Profile;
using Fusion.Resources.Domain.Models;
using MediatR;
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
        ///     Also return departments where the user has no access instead of filtering them out.
        /// </summary>
        public GetRelevantOrgUnits IncludeDepartmentsWithNoAccess()
        {
            OnlyIncludeOrgUnitsWhereUserHasAccess = false;
            return this;
        }

        public GetRelevantOrgUnits SetSiblingDepartmentsAsRelevant()
        {
            IsSiblingDepartmentRelevant = true;
            return this;
        }

        /// <summary>
        /// Mail or azure unique id
        /// </summary>
        public PersonId ProfileId { get; set; }

        public bool OnlyIncludeOrgUnitsWhereUserHasAccess { get; private set; } = true;
        public bool IsSiblingDepartmentRelevant { get; private set; }
        public ODataQueryParams Query { get; }

        public class Handler : IRequestHandler<GetRelevantOrgUnits, QueryRangedList<QueryRelevantOrgUnit>>
        {
            private readonly IMediator mediator;
            private readonly IFusionProfileResolver profileResolver;
            private readonly IOrgUnitCache orgUnitCache;

            public Handler(IMediator mediator, IFusionProfileResolver profileResolver, IOrgUnitCache orgUnitCache)
            {
                this.mediator = mediator;
                this.profileResolver = profileResolver;
                this.orgUnitCache = orgUnitCache;
            }

            public async Task<QueryRangedList<QueryRelevantOrgUnit>> Handle(GetRelevantOrgUnits request, CancellationToken cancellationToken)
            {
                var cachedOrgUnits = await orgUnitCache.GetOrgUnitsAsync();
                var orgUnits = cachedOrgUnits.Select(x => new QueryRelevantOrgUnit
                {
                    SapId = x.SapId,
                    Name = x.Name,
                    FullDepartment = x.FullDepartment,
                    Department = x.Department,
                    ShortName = x.ShortName
                }).ToList();

                var user = await profileResolver.ResolvePersonFullProfileAsync(request.ProfileId.OriginalIdentifier);
                
                if (user?.Roles is null)
                    throw new InvalidOperationException("Roles was not loaded for profile. Required to resolve profile manager responsebility.");


                var orgUnitAccessReason = new List<QueryOrgUnitReason>();

                var managerForUnits = await ResolveUserManagerUnitAsync(user);

                orgUnitAccessReason.AddRange(managerForUnits);


                // Filter out only active roles
                var activeRoles = user.Roles.Where(x => x.IsActive && (x.OnDemandSupport == false || x.ActiveToUtc > DateTime.UtcNow)).ToArray();

                var delegatedManagerClaims = activeRoles.Where(x => x.Name.StartsWith("Fusion.Resources.ResourceOwner")).Where(x => x.Scope?.Value is not null).Select(x => x.Scope?.Value!);
                orgUnitAccessReason.ApplyRole(delegatedManagerClaims, ReasonRoles.DelegatedManager);

                // Must support global roles
                var adminClaims = activeRoles.Where(x => x.Name.StartsWith("Fusion.Resources.Full") || x.Name.StartsWith("Fusion.Resources.Admin")).Select(x => x.Scope?.Value ?? "*");
                orgUnitAccessReason.ApplyRole(adminClaims, ReasonRoles.Write);

                var readClaims = activeRoles.Where(x => x.Name.StartsWith("Fusion.Resources.Request") || x.Name.StartsWith("Fusion.Resources.Read")).Select(x => x.Scope?.Value ?? "*");
                orgUnitAccessReason.ApplyRole(readClaims, ReasonRoles.Read);

                orgUnitAccessReason.ApplyParentAndSiblingManagers(orgUnits, user);

                PopulateOrgUnitReasons(orgUnits, orgUnitAccessReason);

                if (!request.IsSiblingDepartmentRelevant)
                {
                    foreach (var orgUnit in orgUnits)
                        orgUnit.Reasons.RemoveAll(reason => reason is ReasonRoles.SiblingManager or ReasonRoles.DelegatedSiblingManager);
                }

                if (request.OnlyIncludeOrgUnitsWhereUserHasAccess)
                    orgUnits = orgUnits.Where(i => i.Reasons.Any()).ToList();

                var filteredOrgUnits = ApplyOdataFilters(request.Query, orgUnits.OrderBy(x => x.FullDepartment));
                
                var skip = request.Query.Skip.GetValueOrDefault(0);
                var take = request.Query.Top.GetValueOrDefault(100);

                var pagedQuery = QueryRangedList.FromEnumerableItems(filteredOrgUnits.DistinctBy(x => x.SapId), skip, take);

                return pagedQuery;
            }

            private async Task<IEnumerable<QueryOrgUnitReason>> ResolveUserManagerUnitAsync(FusionFullPersonProfile user)
            {
                if (user.Roles is null)
                    throw new InvalidOperationException("Roles was not loaded for profile. Required to resolve profile manager responsebility.");

                var managerRoles = user.Roles
                    .Where(x => string.Equals(x.Name, "Fusion.LineOrg.Manager", StringComparison.OrdinalIgnoreCase))
                    .Where(x => !string.IsNullOrEmpty(x.Scope?.Value))
                    .Select(x => x.Scope?.Value!)
                    .ToList();

                var managerFor = new List<QueryOrgUnitReason>();

                foreach (var orgUnitId in managerRoles)
                {
                    var orgUnit = await mediator.Send(new ResolveLineOrgUnit(orgUnitId));
                    if (orgUnit?.FullDepartment != null)
                    {
                        managerFor.Add(new QueryOrgUnitReason(orgUnit.FullDepartment, ReasonRoles.Manager));
                    }
                }

                return managerFor;
            }

            private static void PopulateOrgUnitReasons(IEnumerable<QueryRelevantOrgUnit> cachedOrgUnits, List<QueryOrgUnitReason> orgUnitAccessReason)
            {

                orgUnitAccessReason.GroupBy(i => i.FullDepartment)
                    .ToList()
                    .ForEach(d =>
                    {
                        var orgUnit = cachedOrgUnits.FirstOrDefault(o => string.Equals(o.FullDepartment, d.Key, StringComparison.OrdinalIgnoreCase));
                        if (orgUnit is not null)
                        {
                            orgUnit.Reasons = d.Select(i => i.Reason).ToList();
                        }
                    });
            }

            private static List<QueryRelevantOrgUnit> ApplyOdataFilters(ODataQueryParams filter, IEnumerable<QueryRelevantOrgUnit> orgUnits)
            {
                var query = orgUnits.AsQueryable()
                    .ApplyODataFilters(filter, m =>
                    {
                        m.SqlQueryMode = false;
                        m.MapField("sapId", e => e.SapId);
                        m.MapField("name", e => e.Name);
                        m.MapField("shortName", e => e.ShortName);
                        m.MapField("department", e => e.Department);
                        m.MapField("fullDepartment", e => e.FullDepartment);

                        // Disabling reasons, as this is not supported by the filter.
                        //m.MapField("reason", e => e.Reasons);
                    })
                    .ApplyODataSorting(filter, m =>
                    {
                        m.MapField("sapId", e => e.SapId);
                        m.MapField("name", e => e.Name);
                        m.MapField("shortName", e => e.ShortName);
                        m.MapField("department", e => e.Department);
                        m.MapField("fullDepartment", e => e.FullDepartment);
                    },
                    q => q.OrderBy(o => o.FullDepartment));

                return query.ToList();
            }
        }
    }

    internal static class OrgUnitAccessReasons
    {

        public static bool IsDirectChildOf(this QueryRelevantOrgUnit orgUnit, QueryOrgUnitReason unit)
        {
            var item = new OrgUnitComparer(orgUnit.FullDepartment);
            var distance = item.Level - unit.Level;

            return item.FullDepartment.StartsWith(unit.FullDepartment) && distance == 1;
        }

        public static bool IsSiblingOf(this QueryRelevantOrgUnit orgUnit, QueryOrgUnitReason unit)
        {
            var orgUnitPath = new DepartmentPath(orgUnit.FullDepartment);
            var unitPath = new DepartmentPath(unit.FullDepartment);

            return orgUnitPath.IsSibling(unitPath);
        }

        internal static void ApplyRole(this List<QueryOrgUnitReason> reasons, IEnumerable<string> departments, string role)
        {
            reasons.AddRange(departments.Select(d => new QueryOrgUnitReason(d, role)));
        }

        internal static void ApplyParentAndSiblingManagers(this List<QueryOrgUnitReason> reasons, List<QueryRelevantOrgUnit> orgUnits, FusionFullPersonProfile user)
        {
            var managerResposibility = new List<QueryOrgUnitReason>();

            // Process locations where user is natural manager. We want to grant the user access to all child and siblings departments.
            foreach (var managerUnit in reasons.Where(x => x.Reason == ReasonRoles.Manager))
            {
                var childDepartments = orgUnits
                    .Where(x => x.FullDepartment.StartsWith(managerUnit.FullDepartment))
                    .Where(x => x.IsDirectChildOf(managerUnit)); // Include trailing space so we do not include the actual unit where user is manager.

                managerResposibility.AddRange(childDepartments.Select(d => new QueryOrgUnitReason(d.FullDepartment, ReasonRoles.ParentManager)));

                var siblingDepartments = orgUnits
                    .Where(x => x.IsSiblingOf(managerUnit));

                managerResposibility.AddRange(siblingDepartments.Select(d => new QueryOrgUnitReason(d.FullDepartment, ReasonRoles.SiblingManager)));
            }

            // Process delegate manager.
            foreach (var delegatedManager in reasons.Where(x => x.Reason == ReasonRoles.DelegatedManager))
            {
                var childDepartments = orgUnits.Where(x => x.FullDepartment.StartsWith(delegatedManager.FullDepartment + " "));

                if (delegatedManager.IsWildCard)
                {
                    // Add all child org units as delegated manager role.
                    managerResposibility.AddRange(childDepartments.Select(d => new QueryOrgUnitReason(d.FullDepartment, ReasonRoles.DelegatedManager)));
                }
                else
                {
                    // Add just direct children
                    managerResposibility.AddRange(childDepartments.Where(d => d.IsDirectChildOf(delegatedManager)).Select(d => new QueryOrgUnitReason(d.FullDepartment, ReasonRoles.DelegatedParentManager)));
                }

                var siblingDepartments = orgUnits
                    .Where(x => x.IsSiblingOf(delegatedManager));
                managerResposibility.AddRange(siblingDepartments.Select(d => new QueryOrgUnitReason(d.FullDepartment, ReasonRoles.DelegatedSiblingManager)));
            }

            // Process read/write roles

            foreach (var delegatedManager in reasons.Where(x => x.Reason == ReasonRoles.Write && x.IsWildCard))
            {
                if (delegatedManager.IsGlobalRole)
                {
                    managerResposibility.AddRange(orgUnits.Select(d => new QueryOrgUnitReason(d.FullDepartment, ReasonRoles.Write)));
                }
                else
                {
                    var childDepartments = orgUnits.Where(x => x.FullDepartment.StartsWith(delegatedManager.FullDepartment) && x.FullDepartment != delegatedManager.FullDepartment);
                    managerResposibility.AddRange(childDepartments.Select(d => new QueryOrgUnitReason(d.FullDepartment, ReasonRoles.Write)));
                }

            }

            foreach (var delegatedManager in reasons.Where(x => x.Reason == ReasonRoles.Read && x.IsWildCard))
            {
                if (delegatedManager.IsGlobalRole)
                {
                    managerResposibility.AddRange(orgUnits.Select(d => new QueryOrgUnitReason(d.FullDepartment, ReasonRoles.Read)));
                }
                else
                {
                    var childDepartments = orgUnits.Where(x => x.FullDepartment.StartsWith(delegatedManager.FullDepartment) && x.FullDepartment != delegatedManager.FullDepartment);
                    managerResposibility.AddRange(childDepartments.Select(d => new QueryOrgUnitReason(d.FullDepartment, ReasonRoles.Read)));
                }
            }

            // Mutate at the end
            reasons.AddRange(managerResposibility);
        }
    }
}