using System;

namespace Fusion.Resources.Integration.Models.Events
{
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
}