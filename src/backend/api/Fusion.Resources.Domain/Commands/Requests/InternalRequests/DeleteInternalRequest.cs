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
            private readonly IMediator mediator;

            public Handler(ResourcesDbContext dbContext, IMediator mediator)
            {
                this.dbContext = dbContext;
                this.mediator = mediator;
            }

            protected override async Task Handle(DeleteInternalRequest request, CancellationToken cancellationToken)
            {
                var req = await dbContext.ResourceAllocationRequests
                    .Include(r => r.Project)
                    .FirstOrDefaultAsync(c => c.Id == request.RequestId);

                var workflow = await dbContext.Workflows.FirstOrDefaultAsync(wf => wf.RequestId == request.RequestId);

                if (req != null)
                    dbContext.ResourceAllocationRequests.Remove(req);
                if (workflow != null)
                    dbContext.Workflows.Remove(workflow);

                await dbContext.SaveChangesAsync();

                if (req is not null)
                    await mediator.Publish(new Notifications.InternalRequests.InternalRequestDeleted(
                        req.Id,
                        req.Project.OrgProjectId,
                        req.OrgPositionId,
                        req.OrgPositionInstance.Id,
                        $"{req.Type}",
                        req.SubType,
                        req.RequestNumber,
                        req.AssignedDepartment, 
                        request.Editor.Person.Name)
                    );

            }
        }
    }
}