using Fusion.Integration;
using Fusion.Integration.LineOrg;
using Fusion.Integration.Profile;
using Fusion.Integration.Roles;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Database;
using Microsoft.EntityFrameworkCore;
using Fusion.Resources.Database.Entities;
using System;

namespace Fusion.Resources.Domain
{
    public class DepartmentHandlerBase
    {
        protected readonly ResourcesDbContext db;
        protected readonly IFusionProfileResolver profileResolver;

        public DepartmentHandlerBase(ResourcesDbContext db, IFusionProfileResolver profileResolver)
        {
            this.db = db;
            this.profileResolver = profileResolver;
        }

        internal async Task ExpandDelegatedResourceOwner(QueryDepartment department, CancellationToken cancellationToken)
        {
            var delegatedResourceOwners = await db.DelegatedDepartmentResponsibles
                .Where(r => r.DepartmentId == department.FullDepartment &&
                 r.DateFrom.Date <= DateTime.UtcNow.Date && r.DateTo.Date >= DateTime.UtcNow.Date)
                .ToListAsync(cancellationToken);

            await ResolveDelegatedOwners(department, delegatedResourceOwners);
        }

        internal async Task ExpandDelegatedResourceOwner(List<QueryDepartment> departments, CancellationToken cancellationToken)
        {
            var departmentIds = departments.Select(x => x.FullDepartment).ToArray();

            var query = await db.DelegatedDepartmentResponsibles
                .Where(r => departmentIds.Contains(r.DepartmentId) &&
                 r.DateFrom.Date <= DateTime.UtcNow.Date && r.DateTo.Date >= DateTime.UtcNow.Date)
                .ToListAsync(cancellationToken);

            var delegatedMap = query.ToLookup(x => x.DepartmentId);

            foreach (var department in departments)
            {
                if (delegatedMap.Contains(department.FullDepartment))
                    await ResolveDelegatedOwners(department, delegatedMap[department.FullDepartment]);
            }
        }

        private async Task ResolveDelegatedOwners(QueryDepartment department, IEnumerable<DbDelegatedDepartmentResponsible>? delegatedResourceOwners)
        {
            if (delegatedResourceOwners is null) return;

            var resolvedProfiles = await profileResolver
                .ResolvePersonsAsync(delegatedResourceOwners.Select(p => new PersonIdentifier(p.ResponsibleAzureObjectId)));

            department.DelegatedResourceOwners = resolvedProfiles
                .Where(res => res.Success)
                .Select(res => res.Profile!)
                .ToList();
        }
    }
}