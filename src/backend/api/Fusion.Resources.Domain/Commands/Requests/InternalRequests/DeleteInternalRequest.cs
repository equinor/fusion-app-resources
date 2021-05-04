using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Events;
using Microsoft.Extensions.Logging;

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
            private readonly IEventNotificationClient notificationClient;
            private readonly ILogger<DeleteInternalRequest> logger;

            public Handler(ResourcesDbContext dbContext, IEventNotificationClient notificationClient, ILogger<DeleteInternalRequest> logger)
            {
                this.dbContext = dbContext;
                this.notificationClient = notificationClient;
                this.logger = logger;
            }

            protected override async Task Handle(DeleteInternalRequest request, CancellationToken cancellationToken)
            {
                var req = await dbContext.ResourceAllocationRequests.FirstOrDefaultAsync(c => c.Id == request.RequestId);
                var workflow = await dbContext.Workflows.FirstOrDefaultAsync(wf => wf.RequestId == request.RequestId);

                if (req != null)
                {
                    var requestToBeDeleted = new QueryResourceAllocationRequest(req);

                    dbContext.ResourceAllocationRequests.Remove(req);
                    if (workflow != null)
                        dbContext.Workflows.Remove(workflow);

                    await dbContext.SaveChangesAsync();

                    await SendNotificationsAsync(requestToBeDeleted);
                }
            }
            private async Task SendNotificationsAsync(QueryResourceAllocationRequest request)
            {
                try
                {
                    var payload = new ResourceAllocationRequestSubscriptionEvent
                    {
                        Type = ResourceAllocationRequestEventType.RequestRemoved,
                        Request = new ResourceAllocationRequestEvent(request),
                        ItemId = request.RequestId
                    };
                    var @event = new FusionEvent<ResourceAllocationRequestSubscriptionEvent>(ResourceAllocationRequestEventTypes.Request, payload);
                    await notificationClient.SendNotificationAsync(@event);
                }
                catch(Exception ex)
                {
                    // Fails if topic doesn't exist
                    logger.LogError(ex.Message);
                }
            }
        }
    }
}
