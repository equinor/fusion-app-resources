using System;

namespace Fusion.Resources.Domain
{
    public class QueryRequestsTimelineItem
    {
        public QueryRequestsTimelineItem(QueryResourceAllocationRequest req)
        {
            Workload = req.OrgPositionInstance?.Workload;
            Id = req.RequestId.ToString();
            PositionName = req.OrgPosition?.Name;
            ProjectName = req.Project.Name;
            AppliesFrom = req.OrgPositionInstance!.AppliesFrom;
            AppliesTo = req.OrgPositionInstance!.AppliesTo;
        }

        public string Id { get; set; } = null!;
        public string? PositionName { get; set; }
        public string? ProjectName { get; set; }
        public double? Workload { get; set; }
        public DateTime AppliesFrom { get; set; }
        public DateTime AppliesTo { get; set; }
    }
}
