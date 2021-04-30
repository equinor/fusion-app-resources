using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Fusion.Events
{
    public class ResourceAllocationRequestSubscriptionEvent
    {
        public Guid ItemId;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ResourceAllocationRequestEvent? Request { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ResourceAllocationRequestEventType Type { get; set; }
    }
}