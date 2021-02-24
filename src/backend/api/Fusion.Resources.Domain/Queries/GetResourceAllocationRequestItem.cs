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
            private readonly IMediator mediator;

            public Handler(ResourcesDbContext db, IProjectOrgResolver orgResolver, IMediator mediator)
            {
                this.db = db;
                this.orgResolver = orgResolver;
                this.mediator = mediator;
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


                if (row is null)
                    return null;

                var workflow = await mediator.Send(new GetRequestWorkflow(request.RequestId));
                var requestItem = new QueryResourceAllocationRequest(row, workflow);

                if (requestItem.OrgPositionId == null) 
                    return requestItem;
                
                var position = await orgResolver.ResolvePositionAsync(requestItem.OrgPositionId.Value);
                if (position != null)
                {
                    requestItem.WithResolvedOriginalPosition(position, requestItem.OrgPositionInstanceId);
                }

                return requestItem;
            }
        }
    }
}
