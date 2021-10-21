using Fusion.Events;
using Fusion.Resources.Domain.Notifications.InternalRequests;
using Fusion.Resources.Integration.Models.FusionEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.FusionEvents.Handlers.InternalRequests
{
    public class RequestDeletedHandler : INotificationHandler<InternalRequestDeleted>
    {
        private readonly IMediator mediator;
        private readonly IEventNotificationClient notificationClient;
        private readonly ILogger<RequestDeletedHandler> logger;

        public RequestDeletedHandler(IMediator mediator, IEventNotificationClient notificationClient, ILogger<RequestDeletedHandler> logger)
        {
            this.mediator = mediator;
            this.notificationClient = notificationClient;
            this.logger = logger;
        }


        public async Task Handle(InternalRequestDeleted notification, CancellationToken cancellationToken)
        {
            if (notification.OrgPositionId is null || notification.PositionInstanceId is null)
                return;

            try
            {
                var payload = new ResourceAllocationRequestSubscriptionEvent
                {
                    Type = EventType.RequestRemoved,
                    ItemId = notification.RequestId,                    
                    Request = new ResourceAllocationRequestEvent(notification.RequestId, notification.OrgProjectId, notification.OrgPositionId.Value, notification.PositionInstanceId.Value, notification.Type, notification.SubType)
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
