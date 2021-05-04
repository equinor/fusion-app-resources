using System;

namespace Fusion.Resources.Integration.Models.Events
{
    public class ResourceAllocationRequestSubscriptionEvent
    {
        public Guid ItemId { get; set; }
        public ResourceAllocationRequestEvent Request { get; set; }
        public ResourceAllocationRequestEventType Type { get; set; }
    }
}