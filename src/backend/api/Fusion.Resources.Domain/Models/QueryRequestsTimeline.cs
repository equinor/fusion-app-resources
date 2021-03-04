using System.Collections.Generic;

namespace Fusion.Resources.Domain
{
    public class QueryRequestsTimeline
    {
        public List<QueryTimelineRange<QueryRequestsTimelineItem>>? Timeline { get; set; }
        public List<QueryResourceAllocationRequest>? Requests { get; set; }

    }
}
