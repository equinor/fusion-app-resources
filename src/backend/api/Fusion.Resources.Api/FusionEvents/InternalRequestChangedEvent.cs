using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Events;
using Fusion.Resources.Domain;
using Fusion.Resources.Integration.Models.Events;
using Microsoft.Extensions.Logging;

namespace Fusion.Resources.Api.FusionEvents
{
    public class InternalRequestChangedEvent : INotification
    {
        public InternalRequestChangedEvent(QueryResourceAllocationRequest request, ResourceAllocationRequestEventType type)
        {
            RequestId = request.RequestId;
            PositionId = request.OrgPositionId!.Value;
            InstanceId = request.OrgPositionInstanceId!.Value;
            Type = type;
        }


        public Guid RequestId { get; }
        public Guid PositionId { get; }
        public Guid InstanceId { get; }
        public ResourceAllocationRequestEventType Type { get; }


        public class Handler : INotificationHandler<InternalRequestChangedEvent>
        {
            private readonly IEventNotificationClient notificationClient;
            private readonly ILogger<InternalRequestChangedEvent> logger;

            public Handler(IEventNotificationClient notificationClient, ILogger<InternalRequestChangedEvent> logger)
            {
                this.notificationClient = notificationClient;
                this.logger = logger;
            }

            public async Task Handle(InternalRequestChangedEvent notification, CancellationToken cancellationToken)
            {
                try
                {
                    var payload = new ResourceAllocationRequestSubscriptionEvent
                    {
                        Type = notification.Type,
                        ItemId = notification.RequestId,
                        Request = new ResourceAllocationRequestEvent(notification.RequestId, notification.PositionId, notification.InstanceId)
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
}
