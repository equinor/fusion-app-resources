using System;

namespace Fusion.Resources.Domain
{
    public class QueryPersonnelTimelineItem
    {
        public QueryPersonnelTimelineItem(string type, QueryPersonAbsenceBasic absence)
        {
            Id = absence.Id;
            Type = type;
            Workload = absence.AbsencePercentage;
            Description = $"{absence.Type}";
            AppliesFrom = absence.AppliesFrom.Date;
            AppliesTo = absence.AppliesTo.GetValueOrDefault(DateTime.MaxValue).Date;

            IsNotePrivate = absence.IsPrivate;
            if (absence.TaskDetails is not null)
            {
                RoleName = absence.TaskDetails.RoleName;
                Location = absence.TaskDetails.Location;
                TaskName = absence.TaskDetails.TaskName;
            }
        }

        public QueryPersonnelTimelineItem(string type, QueryPersonnelPosition position)
        {
            Type = type;
            Workload = position.Workload;
            Id = position.PositionId;
            Description = $"{position.Name}";
            BasePosition = position.BasePosition;
            Project = position.Project;
            AppliesFrom = position.AppliesFrom;
            AppliesTo = position.AppliesTo;
        }

        public QueryPersonnelTimelineItem(string type, QueryResourceAllocationRequest request)
        {
            var instance = request.OrgPositionInstance ?? new ApiClients.Org.ApiPositionInstanceV2();
            Type = type;
            Workload = instance.Workload;
            Id = request.RequestId;
            Description = $"{request.OrgPosition?.Name}";
            BasePosition = (request.OrgPosition?.BasePosition is not null)
                ? new QueryBasePosition(request.OrgPosition.BasePosition) : null;
            Project = new QueryProjectRef(request.Project);
            AppliesFrom = instance.AppliesFrom;
            AppliesTo = instance.AppliesTo;
        }

        public Guid Id { get; set; }
        public string Type { get; set; }
        public double? Workload { get; set; }
        public string Description { get; set; }

        public QueryProjectRef? Project { get; set; }
        public QueryBasePosition? BasePosition { get; set; }
        public DateTime AppliesFrom { get; set; }
        public DateTime AppliesTo { get; set; }

        public bool? IsNotePrivate { get; set; }
        public string? RoleName { get; }
        public string? Location { get; }
        public string? TaskName { get; }
    }
}
