using Fusion.Integration;
using Fusion.Integration.LineOrg;
using Fusion.Integration.Profile;
using Fusion.Integration.Roles;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class DepartmentHandlerBase
    {
        private readonly IFusionRolesClient rolesClient;
        protected readonly ILineOrgResolver lineOrgResolver;
        protected readonly IFusionProfileResolver profileResolver;

        public DepartmentHandlerBase(IFusionRolesClient rolesClient, ILineOrgResolver lineOrgResolver, IFusionProfileResolver profileResolver)
        {
            this.rolesClient = rolesClient;
            this.lineOrgResolver = lineOrgResolver;
            this.profileResolver = profileResolver;
        }

        internal async Task ExpandDelegatedResourceOwner(QueryDepartment department, CancellationToken cancellationToken)
        {
            var delegatedResourceOwners = await rolesClient.GetRolesAsync(q => q
                   .WhereScopeValue(department.DepartmentId))
               ;
            await ResolveDelegatedOwners(department, delegatedResourceOwners);
        }

        internal async Task ExpandDelegatedResourceOwner(List<QueryDepartment> departments, CancellationToken cancellationToken)
        {
            var allDelegatedResourceOwners = (await rolesClient.GetRolesAsync(q => q
             .WhereScopeType("OrgUnit")
             .WhereRoleName(AccessRoles.ResourceOwner))).ToList();

            if (!allDelegatedResourceOwners.Any())
                return;

            foreach (var department in departments)
            {
                var departmentResourceOwners = allDelegatedResourceOwners.Where(x => x.Scope!.Values.Contains(department.DepartmentId));
                await ResolveDelegatedOwners(department, departmentResourceOwners);
            }
        }

        private async Task ResolveDelegatedOwners(QueryDepartment department, IEnumerable<FusionRoleAssignment> delegatedResourceOwners)
        {
            if (delegatedResourceOwners is null) return;

            var resolvedProfiles = await profileResolver
                .ResolvePersonsAsync(delegatedResourceOwners.Select(p => new PersonIdentifier(p.Id)));

            department.DelegatedResourceOwners = resolvedProfiles
                .Where(res => res.Success)
                .Select(res => res.Profile!)
                .ToList();
        }
    }
}