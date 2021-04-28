using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    public class AddDepartment : IRequest<QueryDepartment>
    {
        public AddDepartment(string departmentId, string? sectorId)
        {
            DepartmentId = departmentId.ToUpper();
            SectorId = sectorId?.ToUpper();
        }

        public string DepartmentId { get; }
        public string? SectorId { get; }

        public class Handler : IRequestHandler<AddDepartment, QueryDepartment>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }
            public async Task<QueryDepartment> Handle(AddDepartment request, CancellationToken cancellationToken)
            {
                var entity = new DbDepartment
                {
                    DepartmentId = request.DepartmentId,
                    SectorId = request.SectorId
                };

                db.Add(entity);
                await db.SaveChangesAsync(cancellationToken);

                return new QueryDepartment(entity);
            }
        }
    }
}
