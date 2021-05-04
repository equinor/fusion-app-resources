using System;
using Fusion.Resources.Domain;

namespace Fusion.Events
{
    public class ResourceAllocationRequestEvent
    {
        public ResourceAllocationRequestEvent(QueryResourceAllocationRequest request)
        {
            this.RequestId = request.RequestId;
            this.PositionId = request.OrgPositionId;
            this.InstanceId = request.OrgPositionInstance?.Id;
            this.RequestNumber = request.RequestNumber;
        }
        public long RequestNumber { get; set; }
        public Guid RequestId { get; set; }
        public Guid? PositionId { get; set; }
        public Guid? InstanceId { get; set; }
    }
}