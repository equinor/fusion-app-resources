using System.Collections.Generic;

namespace Fusion.Resources.Domain
{
    public class QueryDepartmentRequestsTimeline
    {
        public List<QueryTimelineRange<DepartmentTimelineItem>>? Timeline { get; set; }
        public List<QueryResourceAllocationRequest>? Requests { get; set; }

        public class DepartmentTimelineItem
        {
            public string Id { get; set; }
            public string? PositionName { get; set; }
            public string? ProjectName { get; set; }
            public double? Workload { get; set; }
        }
    }
}
