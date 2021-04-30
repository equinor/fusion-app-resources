using System.Collections.Generic;

namespace Fusion.Events
{
    public static class ResourceAllocationRequestEventTypes
    {
        public static readonly FusionEventType Request = new("resourceallocation.request");

        public static IEnumerable<string> AllTypes => (IEnumerable<string>)new string[1]
        {
            ResourceAllocationRequestEventTypes.Request.Name
        };
    }
}