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
        private string? departmentFilter { get; set; }
        private string? sector { get; set; }

        public IQueryable<QueryDepartment> Execute(IQueryable<DbDepartment> departments)
        {
            if(!string.IsNullOrEmpty(sector))
            {
                departments = departments.Where(dpt => dpt.SectorId == sector);
            }

            if(!string.IsNullOrEmpty(departmentFilter))
            {
                departments = departments.Where(dpt => dpt.DepartmentId.StartsWith(departmentFilter));
            }

            return departments.Select(dpt => new QueryDepartment(dpt));
        }

        public GetDepartments StartsWith(string department)
        {
            this.departmentFilter = department;
            return this;
        }
        public GetDepartments InSector(string sector)
        {
            this.sector = sector;
            return this;
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
