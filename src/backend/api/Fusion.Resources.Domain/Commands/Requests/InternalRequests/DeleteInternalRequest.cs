using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
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

            public Handler(ResourcesDbContext dbContext)
            {
                this.dbContext = dbContext;
            }

            protected override async Task Handle(DeleteInternalRequest request, CancellationToken cancellationToken)
            {
                var req = await dbContext.ResourceAllocationRequests.FirstOrDefaultAsync(c => c.Id == request.RequestId);
                var workflow = await dbContext.Workflows.FirstOrDefaultAsync(wf => wf.RequestId == request.RequestId);

                if (req != null)
                    dbContext.ResourceAllocationRequests.Remove(req);
                if (workflow != null)
                    dbContext.Workflows.Remove(workflow);

                await dbContext.SaveChangesAsync();
            }
        }
    }
}
