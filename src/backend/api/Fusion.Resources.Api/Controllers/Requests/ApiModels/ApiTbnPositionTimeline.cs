using Fusion.ApiClients.Org;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Queries;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Resources.Api.Controllers.Requests
{
    public class ApiTbnPositionTimeline
    {
        public ApiTbnPositionTimeline(QueryTbnPositionsTimeline timeline, DateTime filterStart, DateTime filterEnd)
        {
            Positions = timeline.Positions?
                .Select(x => new ApiTbnPositionItem(x, filterStart, filterEnd))
                .ToList();
            Timeline = timeline.Timeline;
        }

        public List<ApiTbnPositionItem>? Positions { get; }
        public List<QueryTimelineRange<QueryTbnPositionTimelineItem>>? Timeline { get; }
    }

    public class ApiTbnPositionItem
    {
        public ApiTbnPositionItem(QueryTbnPosition position, DateTime minDateValue, DateTime maxDateValue)
        {
            PositionId = position.PositionId;
            InstanceId = position.InstanceId;
            ParentPositionId = position.ParentPositionId;
            Name = position.Name;
            ProjectId = position.ProjectId;
            BasePosition = position.BasePosition;
            AppliesTo = position.AppliesTo;
            AppliesFrom = position.AppliesFrom;

            FilteredAppliesFrom = position.AppliesFrom < minDateValue ? minDateValue : position.AppliesFrom;
            FilteredAppliesTo = position.AppliesTo > maxDateValue ? maxDateValue : position.AppliesTo;

            Department = position.BasePosition.Department;
            Workload = position.Workload;
            Obs = position.Obs;
            Project = position.Project != null ? new ApiProjectReference(position.Project) : null;
        }
        public Guid PositionId { get; set; }
        public Guid InstanceId { get; set; }
        public string? ParentPositionId { get; set; }

        public string Name { get; set; }
        public Guid ProjectId { get; set; }
        public ApiPositionBasePositionV2 BasePosition { get; set; }

        public DateTime AppliesTo { get; set; }
        public DateTime AppliesFrom { get; set; }

        public DateTime FilteredAppliesTo { get; set; }
        public DateTime FilteredAppliesFrom { get; set; }

        public string? Department { get; set; }
        public double? Workload { get; set; }
        public string? Obs { get; set; }
        public ApiProjectReference? Project { get; internal set; }
    }
}
