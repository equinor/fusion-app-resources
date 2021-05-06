using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Events;
using Fusion.Resources.Domain;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Fusion.Resources.Api.FusionEvents
{
    public class ResourceAllocationRequestChangedEvent : INotification
    {
        public ResourceAllocationRequestChangedEvent(QueryResourceAllocationRequest request, ResourceAllocationRequestEventType type)
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


        public class Handler : INotificationHandler<ResourceAllocationRequestChangedEvent>
        {
            private readonly IEventNotificationClient notificationClient;
            private readonly ILogger<ResourceAllocationRequestChangedEvent> logger;

            public Handler(IEventNotificationClient notificationClient, ILogger<ResourceAllocationRequestChangedEvent> logger)
            {
                this.notificationClient = notificationClient;
                this.logger = logger;
            }

            public async Task Handle(ResourceAllocationRequestChangedEvent notification, CancellationToken cancellationToken)
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
        public class ResourceAllocationRequestEvent
        {
            public ResourceAllocationRequestEvent(Guid requestId, Guid positionId, Guid instanceId)
            {
                this.RequestId = requestId;
                this.PositionId = positionId;
                this.InstanceId = instanceId;
            }
            public Guid RequestId { get; set; }
            public Guid PositionId { get; set; }
            public Guid InstanceId { get; set; }
        }
        public class ResourceAllocationRequestSubscriptionEvent
        {
            public Guid ItemId { get; set; }

            public ResourceAllocationRequestEvent Request { get; set; }

            [JsonConverter(typeof(StringEnumConverter))]
            public ResourceAllocationRequestEventType Type { get; set; }
        }
        public enum ResourceAllocationRequestEventType
        {
            RequestCreated,
            RequestRemoved
        }

    }
}
