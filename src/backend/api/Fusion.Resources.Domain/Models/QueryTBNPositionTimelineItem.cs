using Fusion.Resources.Domain.Queries;
using System;

namespace Fusion.Resources.Domain
{
    public class QueryTbnPositionTimelineItem
    {
        public QueryTbnPositionTimelineItem(QueryTbnPosition tbnPosition)
        {
            Id = $"{tbnPosition.InstanceId}";
            ProjectId = tbnPosition.ProjectId;
            PositionName = tbnPosition.Name;
            Workload = tbnPosition.Workload;
        }

        public QueryTbnPositionTimelineItem(Guid id, Guid projectId, string name, double? workload)
        {
            Id = $"{id}";
            ProjectId = projectId;
            PositionName = name;
            Workload = workload;
        }

        public string Id { get; set; } = null!;
        public string PositionName { get; set; } = null!;
        public Guid ProjectId { get; set; }
        public double? Workload { get; set; }
    }
}
