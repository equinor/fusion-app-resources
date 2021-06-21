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

            if(absence.TaskDetails is not null)
            {
                RoleName = absence.TaskDetails.RoleName;
                Location = absence.TaskDetails.Location;
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

        public Guid Id { get; set; }
        public string Type { get; set; } 
        public double? Workload { get; set; }
        public string Description { get; set; }

        public QueryProjectRef? Project { get; set; }
        public QueryBasePosition? BasePosition { get; set; }
        public DateTime AppliesFrom { get; set; }
        public DateTime AppliesTo { get; set; }
        public string? RoleName { get; }
        public string? Location { get; }
    }
}
