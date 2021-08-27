using Fusion.Resources.Database.Entities;
using MediatR;
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

        public class Handler : IRequestHandler<GetDepartmentSector, string?>
        {
            public Task<string?> Handle(GetDepartmentSector query, CancellationToken cancellationToken)
            {
                var path = new DepartmentPath(query.departmentId);
                var sector = (path.Level > 1) ? path.Parent() : null;
                return Task.FromResult(sector);
            }
        }
    }
}
