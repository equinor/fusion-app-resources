using Fusion.Integration;
using Fusion.Integration.LineOrg;
using Fusion.Integration.Profile;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class DepartmentHandlerBase
    {
        protected readonly ResourcesDbContext db;
        protected readonly ILineOrgResolver lineOrgResolver;
        protected readonly IFusionProfileResolver profileResolver;
        public DepartmentHandlerBase(ResourcesDbContext db, ILineOrgResolver lineOrgResolver, IFusionProfileResolver profileResolver)
        {
            this.db = db;
            this.lineOrgResolver = lineOrgResolver;
            this.profileResolver = profileResolver;
        }

        internal async Task ExpandDelegatedResourceOwner(QueryDepartment department, CancellationToken cancellationToken)
        {
            var delegatedResourceOwners = await db.DepartmentResponsibles
                            .Where(r => r.DepartmentId == department.DepartmentId)
                            .ToListAsync(cancellationToken);

            await ResolveDelegatedOwners(department, delegatedResourceOwners);
        }

        internal async Task ExpandDelegatedResourceOwner(List<QueryDepartment> departments, CancellationToken cancellationToken)
        {
            var departmentIds = departments.Select(x => x.DepartmentId).ToArray();

            var query = await db.DepartmentResponsibles
                            .Where(r => departmentIds.Contains(r.DepartmentId))
                            .ToListAsync(cancellationToken);

            var delegatedMap = query.ToLookup(x => x.DepartmentId);

            foreach (var department in departments)
            {
                if (delegatedMap.Contains(department.DepartmentId))
                    await ResolveDelegatedOwners(department, delegatedMap[department.DepartmentId]);
            }
        }

        private async Task ResolveDelegatedOwners(QueryDepartment department, IEnumerable<DbDepartmentResponsible> delegatedResourceOwners)
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
