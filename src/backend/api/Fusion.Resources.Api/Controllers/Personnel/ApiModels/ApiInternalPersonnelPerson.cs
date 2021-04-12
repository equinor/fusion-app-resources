using Fusion.Integration.Profile;
using Fusion.Resources.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiInternalPersonnelPerson
    {
        public ApiInternalPersonnelPerson(QueryInternalPersonnelPerson p)
        {
            AzureUniquePersonId = p.AzureUniqueId;
            Mail = p.Mail!;
            Name = p.Name;
            AccountType = p.AccountType;
            PhoneNumber = p.PhoneNumber;
            JobTitle = p.JobTitle;
            OfficeLocation = p.OfficeLocation;
            Department = p.Department!;
            FullDepartment = p.FullDepartment!;
            IsResourceOwner = p.IsResourceOwner;

            if (p.Timeline != null) Timeline = p.Timeline.Select(ti => new TimelineRange(ti)).ToList();

            PositionInstances = p.PositionInstances.Select(pos => new PersonnelPosition(pos)).ToList();
            EmploymentStatuses = p.Absence.Select(a => new PersonnelAbsence(a)).ToList();

            Disciplines = p.PositionInstances
                .OrderByDescending(p => p.AppliesTo)
                .Select(p => p.BasePosition.Discipline)
                .Where(d => !string.IsNullOrEmpty(d))
                .Distinct()
                .ToList();
        }

        public Guid? AzureUniquePersonId { get; set; }
        public string Mail { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? JobTitle { get; set; }
        public string? OfficeLocation { get; set; }
        public string Department { get; set; }
        public string FullDepartment { get; set; }
        public bool IsResourceOwner { get; set; }

        /// <summary>
        /// Enum, <see cref="FusionAccountType"/>.
        /// </summary>
        public string AccountType { get; set; }

        public List<string> Disciplines { get; set; } = new List<string>();

        public List<PersonnelPosition> PositionInstances { get; set; } = new List<PersonnelPosition>();
        public List<PersonnelAbsence> EmploymentStatuses { get; set; } = new List<PersonnelAbsence>();

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<TimelineRange>? Timeline { get; set; }


        public class TimelineRange
        {
            public TimelineRange(QueryTimelineRange<QueryPersonnelTimelineItem> ti)
            {
                AppliesFrom = ti.AppliesFrom;
                AppliesTo = ti.AppliesTo;
                Workload = ti.Workload;

                Items = ti.Items.Select(i => new TimelineItem(i)).ToList();
            }

            public DateTime AppliesFrom { get; set; }
            public DateTime AppliesTo { get; set; }
            public List<TimelineItem> Items { get; set; } = new List<TimelineItem>();
            public double Workload { get; set; }
        }

        public class TimelineItem
        {
            public TimelineItem(QueryPersonnelTimelineItem item)
            {
                Id = item.Id;
                Workload = item.Workload;
                Type = item.Type;
                Description = item.Description;

                if (item.Project != null) Project = new ApiProjectReference(item.Project);
                if (item.BasePosition != null) BasePosition = new ApiBasePosition(item.BasePosition);
            }

            public Guid Id { get; set; }
            public string Type { get; set; } = null!;
            public double? Workload { get; set; }
            public string Description { get; set; } = null!;

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public ApiProjectReference? Project { get; set; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public ApiBasePosition? BasePosition { get; set; }
        }

        public class PersonnelPosition
        {
            public PersonnelPosition(QueryPersonnelPosition pos)
            {
                PositionId = pos.PositionId;
                InstanceId = pos.InstanceId;
                AppliesFrom = pos.AppliesFrom;
                AppliesTo = pos.AppliesTo;
                Name = pos.Name;
                Location = pos.Location;
                Workload = pos.Workload;
                AllocationState = pos.AllocationState;
                AllocationUpdated = pos.AllocationUpdated;

                Project = new ApiProjectReference(pos.Project);
                BasePosition = new ApiBasePosition(pos.BasePosition);
            }

            public Guid PositionId { get; set; }
            public Guid InstanceId { get; set; }
            public DateTime AppliesFrom { get; set; }
            public DateTime AppliesTo { get; set; }

            public ApiBasePosition BasePosition { get; set; } = null!;
            public string Name { get; set; } = null!;
            public string? Location { get; set; }
            public string? AllocationState { get; set; }
            public DateTime? AllocationUpdated { get; set; }

            public bool IsActive => AppliesFrom >= DateTime.UtcNow.Date && AppliesTo >= DateTime.UtcNow.Date;
            public double Workload { get; set; }
            public ApiProjectReference Project { get; set; } = null!;
        }
        public class PersonnelAbsence
        {
            public PersonnelAbsence(QueryPersonAbsenceBasic absence)
            {
                Id = absence.Id;
                AppliesFrom = absence.AppliesFrom.UtcDateTime;
                AppliesTo = absence.AppliesTo is null ? absence.AppliesFrom.UtcDateTime : absence.AppliesTo!.Value.UtcDateTime;
                Type = $"{absence.Type}";
                AbsencePercentage = absence.AbsencePercentage;
            }

            public Guid Id { get; set; }
            public DateTime AppliesFrom { get; set; }
            public DateTime AppliesTo { get; set; }
            public double? AbsencePercentage { get; set; }
            public string Type { get; set; } = null!;
        }
    }

}
