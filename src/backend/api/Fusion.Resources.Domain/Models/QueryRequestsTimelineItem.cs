using System;

namespace Fusion.Resources.Domain
{
    public class QueryRequestsTimelineItem
    {
        public string Id { get; set; } = null!;
        public string? PositionName { get; set; }
        public string? ProjectName { get; set; }
        public double? Workload { get; set; }
        public DateTime AppliesFrom { get; set; }
        public DateTime AppliesTo { get; set; }
    }
}
