using System;

namespace Fusion.Resources.Domain
{
    public class QueryTBNPositionTimelineItem
    {
        public string Id { get; set; } = null!;
        public string? PositionName { get; set; }
        public Guid ProjectId { get; set; }
        public double? Workload { get; set; }
    }
}
