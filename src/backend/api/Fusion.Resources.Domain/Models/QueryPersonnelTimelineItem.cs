using System;

namespace Fusion.Resources.Domain
{
    public class QueryPersonnelTimelineItem
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = null!;
        public double? Workload { get; set; }
        public string Description { get; set; } = null!;

        public QueryProjectRef? Project { get; set; }
        public QueryBasePosition? BasePosition { get; set; }
        public DateTime AppliesFrom { get; set; }
        public DateTime AppliesTo { get; set; }
    }
}
