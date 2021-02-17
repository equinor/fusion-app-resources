using Fusion.Resources.Database;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Integration.Org;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Resources.Domain.Queries
{
    public class GetResourceAllocationRequestItem : IRequest<QueryResourceAllocationRequest?>
    {
        public GetResourceAllocationRequestItem(Guid requestId)
        {
            RequestId = requestId;
        }

        public Guid RequestId { get; }

        public class Handler : IRequestHandler<GetResourceAllocationRequestItem, QueryResourceAllocationRequest?>
        {
            private readonly ResourcesDbContext db;
            private readonly IProjectOrgResolver orgResolver;

            public Handler(ResourcesDbContext db, IProjectOrgResolver orgResolver)
            {
                this.db = db;
                this.orgResolver = orgResolver;
            }

            public async Task<QueryResourceAllocationRequest?> Handle(GetResourceAllocationRequestItem request, CancellationToken cancellationToken)
            {
                var row = await db.ResourceAllocationRequests
                    .Include(r => r.OrgPositionInstance)
                    .Include(r => r.CreatedBy)
                    .Include(r => r.UpdatedBy)
                    .Include(r => r.Project)
                    .Include(r => r.ProposedPerson)
                    .FirstOrDefaultAsync(c => c.Id == request.RequestId);

                var requestItem = row != null ? new QueryResourceAllocationRequest(row) : null;

                if (requestItem?.OrgPositionId != null)
                {
                    var position = await orgResolver.ResolvePositionAsync(requestItem.OrgPositionId.Value);
                    if (position != null)
                    {
                        requestItem.WithResolvedOriginalPosition(position, requestItem.OrgPositionInstanceId);
                    }
                }

                return requestItem;
            }
        }
    }
}
