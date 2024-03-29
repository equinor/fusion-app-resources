﻿using Fusion.AspNetCore.OData;
using Fusion.Integration;
using Fusion.Integration.Profile;
using Fusion.Resources.Domain.Models;
using MediatR;
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
            private readonly IOrgUnitCache orgUnitCache;

            public Handler(IFusionProfileResolver profileResolver, IOrgUnitCache orgUnitCache)
            {
                this.profileResolver = profileResolver;
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

                var orgUnitAccessReason = new List<QueryOrgUnitReason>();

                orgUnitAccessReason.ApplyManager(user);

                // Filter out only active roles
                var activeRoles = user.Roles.Where(x => x.IsActive);

                var delegatedManagerClaims = activeRoles.Where(x => x.Name.StartsWith("Fusion.Resources.ResourceOwner")).Select(x => x.Scope?.Value);
                orgUnitAccessReason.ApplyRole(delegatedManagerClaims, ReasonRoles.DelegatedManager);

                var adminClaims = activeRoles?.Where(x => x.Name.StartsWith("Fusion.Resources.Full") || x.Name.StartsWith("Fusion.Resources.Admin")).Select(x => x.Scope?.Value);
                orgUnitAccessReason.ApplyRole(adminClaims, ReasonRoles.Write);

                var readClaims = activeRoles?.Where(x => x.Name.StartsWith("Fusion.Resources.Request") || x.Name.StartsWith("Fusion.Resources.Read")).Select(x => x.Scope?.Value);
                orgUnitAccessReason.ApplyRole(readClaims, ReasonRoles.Read);

                orgUnitAccessReason.ApplyParentManager(orgUnits, user);

                if (orgUnitAccessReason is null)
                {
                    return null;
                }

                List<QueryRelevantOrgUnit> populatedOrgUnitResult = GetRelevantOrgUnits(orgUnits, orgUnitAccessReason);

                var filteredOrgUnits = ApplyOdataFilters(request.Query, populatedOrgUnitResult.OrderBy(x => x.SapId));
                var skip = request.Query.Skip.GetValueOrDefault(0);
                var take = request.Query.Top.GetValueOrDefault(100);

                var pagedQuery = QueryRangedList.FromEnumerableItems(filteredOrgUnits.DistinctBy(x => x.SapId), skip, take);

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
                            QueryRelevantOrgUnit? data = new();
                            if (org.IsWildCard == true)
                            {
                                data = cachedOrgUnits.FirstOrDefault(x => x.FullDepartment == org.FullDepartment.Replace('*', ' ').TrimEnd());
                            }
                            else
                            {
                                data = cachedOrgUnits.FirstOrDefault(x => x.FullDepartment == org.FullDepartment || x.Department == org.FullDepartment);
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

        }
    }

    internal static class OrgUnitAccessReasons
    {
        internal static void ApplyManager(this List<QueryOrgUnitReason> reasons, FusionFullPersonProfile user)
        {
            var isDepartmentManager = user.IsResourceOwner;
            if (isDepartmentManager)
                reasons.Add(new QueryOrgUnitReason
                {
                    FullDepartment = user?.FullDepartment ?? "",
                    Reason = ReasonRoles.Manager
                });
        }

        internal static void ApplyRole(this List<QueryOrgUnitReason> reasons, IEnumerable<string?>? departments, string role)
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

        internal static void ApplyParentManager(this List<QueryOrgUnitReason> reasons, IEnumerable<QueryRelevantOrgUnit> orgUnits, FusionFullPersonProfile user)
        {
            var managerResposibility = new List<QueryOrgUnitReason>();
            var managerOrDelegatedManagerDepartmentsOrWildcard = reasons
                .Where(x => x.Reason.Equals(ReasonRoles.Manager) || x.Reason.Equals(ReasonRoles.DelegatedManager) || x.IsWildCard).ToList();
            if (managerOrDelegatedManagerDepartmentsOrWildcard is not null)
            {
                foreach (var department in managerOrDelegatedManagerDepartmentsOrWildcard)
                {
                    var parentDepartment = department.FullDepartment.Replace("*", "").TrimEnd();

                    var childDepartments = orgUnits.Distinct().Where(x => x.FullDepartment.StartsWith(parentDepartment) && !x.FullDepartment.Equals(parentDepartment));

                    // if the department is not of type wildcard we only want to get direct children (one level below)
                    if (!department.IsWildCard)
                    {
                        var getParentDepartmentLevel = GetAcronymsForDepartment(parentDepartment);

                        childDepartments = childDepartments.Where(x => (GetAcronymsForDepartment(x.FullDepartment).Count() == getParentDepartmentLevel.Length + 1));

                    }
                    var reason = ReasonRoles.DelegatedParentManager;
                    if (user.IsResourceOwner && user.FullDepartment == parentDepartment)
                    {
                        reason = ReasonRoles.ParentManager;
                    }

                    foreach (var child in childDepartments)
                    {
                        managerResposibility?.Add(new QueryOrgUnitReason
                        {
                            FullDepartment = child.FullDepartment,
                            Reason = reason
                        });
                    }
                }
                reasons?.AddRange(managerResposibility);
            }
        }

        static string[] GetAcronymsForDepartment(string word)
        {
            return word.Split();
        }
    }
}