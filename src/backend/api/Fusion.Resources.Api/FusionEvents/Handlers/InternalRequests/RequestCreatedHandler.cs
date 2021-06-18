using Fusion.Events;
using Fusion.Resources.Domain.Notifications.InternalRequests;
using Fusion.Resources.Integration.Models.FusionEvents;
using Fusion.Resources.Logic.Commands;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.FusionEvents.Handlers.InternalRequests
{
    public class RequestCreatedHandler : INotificationHandler<InternalRequestCreated>
    {
        private readonly IMediator mediator;
        private readonly IEventNotificationClient notificationClient;
        private readonly ILogger<RequestCreatedHandler> logger;

        public RequestCreatedHandler(IMediator mediator, IEventNotificationClient notificationClient, ILogger<RequestCreatedHandler> logger)
        {
            this.mediator = mediator;
            this.notificationClient = notificationClient;
            this.logger = logger;
        }

        public async Task Handle(InternalRequestCreated notification, CancellationToken cancellationToken)
        {
            var req = await mediator.Send(new Domain.Queries.GetResourceAllocationRequestItem(notification.RequestId));

            if (req is null || req.OrgPositionId is null || req.OrgPositionInstanceId is null)
                return;

            try
            {
                var payload = new ResourceAllocationRequestSubscriptionEvent
                {
                    Type = EventType.RequestCreated,
                    ItemId = notification.RequestId,
                    Request = new ResourceAllocationRequestEvent(notification.RequestId, req.Project.OrgProjectId, req.OrgPositionId.Value, req.OrgPositionInstanceId.Value, $"{req.Type}", req.SubType)
                };
                var @event = new FusionEvent<ResourceAllocationRequestSubscriptionEvent>(new FusionEventType("resourceallocation.request"), payload);
                await notificationClient.SendNotificationAsync(@event);
            }
            catch (Exception ex)
            {
                // Fails if topic doesn't exist
                logger.LogError(ex.Message);
            }
        }
    }
}
