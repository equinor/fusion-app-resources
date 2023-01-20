using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    public class DeleteInternalRequest : TrackableRequest
    {
        public DeleteInternalRequest(Guid requestId)
        {
            RequestId = requestId;
        }


        private Guid RequestId { get; }


        public class Handler : AsyncRequestHandler<DeleteInternalRequest>
        {
            private readonly ResourcesDbContext dbContext;
            private readonly IMediator mediator;

            public Handler(ResourcesDbContext dbContext, IMediator mediator)
            {
                this.dbContext = dbContext;
                this.mediator = mediator;
            }

            protected override async Task Handle(DeleteInternalRequest request, CancellationToken ct)
            {
                var req = await dbContext.ResourceAllocationRequests
                    .Include(r => r.Project)
                    .Include(r => r.SecondOpinions).ThenInclude(x => x.Responses)
                    .AsSingleQuery()//https://learn.microsoft.com/nb-no/ef/core/querying/single-split-queries -- Should request for all required data in single query.
                    .FirstOrDefaultAsync(c => c.Id == request.RequestId, ct);
                
                if (req is null) return;

                var workflow = await dbContext.Workflows.FirstOrDefaultAsync(wf => wf.RequestId == request.RequestId, ct);

                dbContext.RemoveRange(req.SecondOpinions.SelectMany(x => x.Responses!));
                dbContext.RemoveRange(req.SecondOpinions);

                if (req != null)
                    dbContext.ResourceAllocationRequests.Remove(req);
                if (workflow != null)
                    dbContext.Workflows.Remove(workflow);


                await dbContext.SaveChangesAsync(ct);

                if (req is not null)
                {
                    await mediator.Publish(new Notifications.InternalRequests.InternalRequestDeleted(new QueryResourceAllocationRequest(req), request.Editor.Person.Name));
                }
            }
        }
    }
}