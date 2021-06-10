using Fusion.Integration;
using Fusion.Integration.Profile;
using Fusion.Resources.Application.LineOrg;
using Fusion.Resources.Database;
using Microsoft.EntityFrameworkCore;
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

            if (delegatedResourceOwners is not null)
            {
                var resolvedProfiles = await profileResolver
                    .ResolvePersonsAsync(delegatedResourceOwners.Select(p => new PersonIdentifier(p.ResponsibleAzureObjectId)));

                department.DelegatedResourceOwners = resolvedProfiles
                    .Where(res => res.Success)
                    .Select(res => res.Profile!)
                    .ToList();
            }
        }
    }
}
