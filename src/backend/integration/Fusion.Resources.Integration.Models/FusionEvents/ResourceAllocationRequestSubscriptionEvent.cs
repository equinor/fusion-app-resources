using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace Fusion.Resources.Integration.Models.FusionEvents
{
    public class ResourceAllocationRequestSubscriptionEvent
    {
        public Guid ItemId { get; set; }

        public ResourceAllocationRequestEvent Request { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public EventType Type { get; set; }
    }
}
