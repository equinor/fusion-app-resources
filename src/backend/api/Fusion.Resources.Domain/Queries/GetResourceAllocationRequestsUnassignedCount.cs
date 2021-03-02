using System.Linq;
using Fusion.Resources.Database;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Resources.Domain.Queries
{
    public class GetResourceAllocationRequestsUnassignedCount : IRequest<int>
    {
        public GetResourceAllocationRequestsUnassignedCount(bool? isDraft)
        {
            IsDraft = isDraft;
        }

        public bool? IsDraft { get; }


        public class Handler : IRequestHandler<GetResourceAllocationRequestsUnassignedCount, int>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            public async Task<int> Handle(GetResourceAllocationRequestsUnassignedCount request, CancellationToken cancellationToken)
            {
                var query = db.ResourceAllocationRequests
                    .Where(x => string.IsNullOrEmpty(x.AssignedDepartment))
                    .AsQueryable();

                if (request.IsDraft.HasValue)
                    query = query.Where(x => x.IsDraft == request.IsDraft.Value);

                return await query.CountAsync();
            }
        }
    }
}
