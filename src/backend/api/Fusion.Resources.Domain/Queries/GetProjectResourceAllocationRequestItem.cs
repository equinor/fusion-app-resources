using Fusion.Resources.Database;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fusion.ApiClients.Org;
using Fusion.Integration.Org;
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
            private readonly IProjectOrgResolver orgResolver;

            public Handler(ResourcesDbContext db, IProjectOrgResolver orgResolver)
            {
                this.db = db;
                this.orgResolver = orgResolver;
            }

            public async Task<QueryResourceAllocationRequest?> Handle(GetProjectResourceAllocationRequestItem request, CancellationToken cancellationToken)
            {
                var row = await db.ResourceAllocationRequests
                    .Include(r => r.OrgPositionInstance)
                    .Include(r => r.CreatedBy)
                    .Include(r => r.UpdatedBy)
                    .Include(r => r.Project)
                    .Include(r => r.ProposedPerson)
                    .FirstOrDefaultAsync(c => c.Id == request.RequestId);

                var requestItem = row != null ? new QueryResourceAllocationRequest(row) : null;

                if (requestItem?.OrgPosition != null)
                {
                    var position = await orgResolver.ResolvePositionAsync(requestItem.OrgPosition.Id);
                    if (position != null)
                    {
                        requestItem.WithResolvedOriginalPosition(position);
                    }
                }

                return requestItem;
            }
        }
    }
}
