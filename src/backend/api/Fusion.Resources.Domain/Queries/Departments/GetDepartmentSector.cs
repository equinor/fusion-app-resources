using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Queries
{
    public class GetDepartmentSector : IRequest<string?>
    {
        private readonly string departmentId;

        public GetDepartmentSector(string departmentId)
        {
            this.departmentId = departmentId;
        }

        public IQueryable<string?> Execute(IQueryable<DbDepartment> departments)
        {
            return departments
                .Where(dpt => dpt.DepartmentId == departmentId)
                .Select(dpt => dpt.SectorId);
        }

        public class Handler : IRequestHandler<GetDepartmentSector, string?>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            public async Task<string?> Handle(GetDepartmentSector query, CancellationToken cancellationToken)
            {
                var path = new DepartmentPath(query.departmentId);
                return (path.Level > 1) ? path.Parent() : null;
                //return await query.Execute(db.Departments).FirstOrDefaultAsync(cancellationToken);
            }
        }
    }
}
