using Fusion.Resources.Database;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Domain.Commands;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Resources.Domain.Queries
{
    public class GetProjectResourceAllocationRequestItem : IRequest<QueryResourceAllocationRequest>
    {
        public GetProjectResourceAllocationRequestItem(Guid requestId)
        {
            RequestId = requestId;
        }

        public Guid RequestId { get; }

        public class Handler : IRequestHandler<GetProjectResourceAllocationRequestItem, QueryResourceAllocationRequest?>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            public async Task<QueryResourceAllocationRequest?> Handle(GetProjectResourceAllocationRequestItem request, CancellationToken cancellationToken)
            {
                var row = await db.ResourceAllocationRequests
                    .Include(r => r.ResourceAllocationOrgPositionInstance)
                    .Include(r => r.CreatedBy)
                    .Include(r => r.UpdatedBy)
                    .Include(r => r.Project)
                    .Include(r => r.ProposedPerson)
                    .FirstOrDefaultAsync(c => c.Id == request.RequestId);
                return row != null ? new QueryResourceAllocationRequest(row) : null;
            }
        }
    }
}
