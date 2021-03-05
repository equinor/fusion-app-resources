using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class GetDepartments : IRequest<IEnumerable<QueryDepartment>>
    {
        public string? DepartmentFilter { get; set; }
        public string Sector { get; set; }

        public IQueryable<QueryDepartment> Execute(IQueryable<DbDepartment> departments)
        {
            if(!string.IsNullOrEmpty(Sector))
            {
                departments = departments.Where(dpt => dpt.SectorId == Sector);
            }

            if(!string.IsNullOrEmpty(DepartmentFilter))
            {
                departments = departments.Where(dpt => dpt.DepartmentId.StartsWith(DepartmentFilter));
            }

            return departments.Select(dpt => new QueryDepartment(dpt));
        }

        public class Handler : IRequestHandler<GetDepartments, IEnumerable<QueryDepartment>>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            public async Task<IEnumerable<QueryDepartment>> Handle(GetDepartments request, CancellationToken cancellationToken)
            {
                return await request.Execute(db.Departments).ToListAsync(cancellationToken);
            }
        }
    }
}
