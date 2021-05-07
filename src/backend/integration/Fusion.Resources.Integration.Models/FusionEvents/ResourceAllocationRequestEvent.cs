using System;
using System.Collections.Generic;
using System.Text;

namespace Fusion.Resources.Integration.Models.FusionEvents
{
    public class ResourceAllocationRequestEvent
    {
        public ResourceAllocationRequestEvent(Guid requestId, Guid orgChartId, Guid positionId, Guid instanceId)
        {
            RequestId = requestId;
            OrgChartId = orgChartId;
            PositionId = positionId;
            InstanceId = instanceId;
        }

        public Guid RequestId { get; set; }

        /// <summary>
        /// Aka project id in org chart service.
        /// </summary>
        public Guid OrgChartId { get; set; }
        public Guid PositionId { get; set; }
        public Guid InstanceId { get; set; }
    }
}
