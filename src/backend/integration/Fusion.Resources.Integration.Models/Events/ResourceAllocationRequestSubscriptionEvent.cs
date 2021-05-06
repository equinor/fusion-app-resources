using System;
using System.Text.Json.Serialization;

namespace Fusion.Resources.Integration.Models.Events
{
    public class ResourceAllocationRequestSubscriptionEvent
    {
        public Guid ItemId { get; set; }

        public ResourceAllocationRequestEvent Request { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ResourceAllocationRequestEventType Type { get; set; }
    }
}