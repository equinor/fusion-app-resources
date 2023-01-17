using Fusion.Integration;
using Fusion.Integration.LineOrg;
using Fusion.Integration.Profile;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Database;
using Microsoft.EntityFrameworkCore;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain.Models;

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
            var delegatedResourceOwners = await db.DelegatedDepartmentResponsibles
                .Where(r => r.DepartmentId == department.DepartmentId)
                .ToListAsync(cancellationToken);

            await ResolveDelegatedOwners(department, delegatedResourceOwners);
        }

        internal async Task ExpandDelegatedResourceOwner(List<QueryDepartment> departments, CancellationToken cancellationToken)
        {
            var departmentIds = departments.Select(x => x.DepartmentId).ToArray();

            var query = await db.DelegatedDepartmentResponsibles
                .Where(r => departmentIds.Contains(r.DepartmentId))
                .ToListAsync(cancellationToken);

            var delegatedMap = query.ToLookup(x => x.DepartmentId);

            foreach (var department in departments)
            {
                if (delegatedMap.Contains(department.DepartmentId))
                    await ResolveDelegatedOwners(department, delegatedMap[department.DepartmentId].ToList());
            }
        }

        private async Task ResolveDelegatedOwners(QueryDepartment department,  IList<DbDelegatedDepartmentResponsible>? delegatedResourceOwners)
        {
            if (delegatedResourceOwners is null) return;

            var profilesToResolve = delegatedResourceOwners
                .Select(p => new PersonIdentifier(p.ResponsibleAzureObjectId)).ToList()
                .Union(delegatedResourceOwners.Select(p => new PersonIdentifier(p.UpdatedBy.GetValueOrDefault())));

            var resolvedProfiles = await profileResolver
                .ResolvePersonsAsync(profilesToResolve);

            var actualProfiles = resolvedProfiles
                .Where(res => res.Success)
                .Select(res => res.Profile!)
                .ToList();

            department.DelegatedResourceOwners = delegatedResourceOwners?.Select(x => new QueryDepartmentResponsible(x)).ToList() ?? new List<QueryDepartmentResponsible>();

            foreach (var ro in department.DelegatedResourceOwners)
            {
                ro.DelegatedResponsible = actualProfiles.FirstOrDefault(x => x.AzureUniqueId == ro.AzureAdObjectId);
                ro.CreatedBy = actualProfiles.FirstOrDefault(x => x.AzureUniqueId == ro.CreatedByAzureUniqueId);
            }
        }
    }
}