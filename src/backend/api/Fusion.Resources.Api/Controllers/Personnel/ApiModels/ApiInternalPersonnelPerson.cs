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
        private ApiInternalPersonnelPerson(QueryInternalPersonnelPerson p)
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

            PositionInstances = p.PositionInstances.Select(pos => new PersonnelPosition(pos)).ToList();
            Disciplines = p.PositionInstances
                .OrderByDescending(p => p.AppliesTo)
                .Select(p => p.BasePosition.Discipline)
                .Where(d => !string.IsNullOrEmpty(d))
                .Distinct()
                .ToList();
            PendingRequests = p.PendingRequests != null
                ? p.PendingRequests.Select(x => new ApiResourceAllocationRequest(x)).ToList()
                : new();
        }

        public static ApiInternalPersonnelPerson CreateWithoutConfidentialTaskInfo(QueryInternalPersonnelPerson person) 
            => new ApiInternalPersonnelPerson(person)
            {
                EmploymentStatuses = person.Absence.Select(a => ApiPersonAbsence.CreateWithoutConfidentialTaskInfo(a)).ToList(),
                Timeline = person?.Timeline?.Select(ti => TimelineRange.CreateWithoutConfidentialTaskInfo(ti))?.ToList()
            };
        public static ApiInternalPersonnelPerson CreateWithConfidentialTaskInfo(QueryInternalPersonnelPerson person)
            => new ApiInternalPersonnelPerson(person)
            {
                EmploymentStatuses = person.Absence.Select(a => ApiPersonAbsence.CreateWithConfidentialTaskInfo(a)).ToList(),
                Timeline = person?.Timeline?.Select(ti => TimelineRange.CreateWithConfidentialTaskInfo(ti))?.ToList()
            };


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
        public List<ApiResourceAllocationRequest> PendingRequests { get; }
        public List<PersonnelPosition> PositionInstances { get; set; } = new List<PersonnelPosition>();
        public List<ApiPersonAbsence> EmploymentStatuses { get; set; } = new List<ApiPersonAbsence>();

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<TimelineRange>? Timeline { get; set; }


        public class TimelineRange
        {
            private TimelineRange(QueryTimelineRange<QueryPersonnelTimelineItem> ti)
            {
                AppliesFrom = ti.AppliesFrom;
                AppliesTo = ti.AppliesTo;
                Workload = ti.Workload;

            }

            public static TimelineRange CreateWithoutConfidentialTaskInfo(QueryTimelineRange<QueryPersonnelTimelineItem> item)
                => new TimelineRange(item)
                {
                    Items = item.Items.Select(i => TimelineItem.CreateWithoutConfidentialTaskInfo(i)).ToList()
                };
            public static TimelineRange CreateWithConfidentialTaskInfo(QueryTimelineRange<QueryPersonnelTimelineItem> item)
                => new TimelineRange(item)
                {
                    Items = item.Items.Select(i => TimelineItem.CreateWithConfidentialTaskInfo(i)).ToList()
                };


            public DateTime AppliesFrom { get; set; }
            public DateTime AppliesTo { get; set; }
            public List<TimelineItem> Items { get; set; } = new List<TimelineItem>();
            public double Workload { get; set; }
        }

        public class TimelineItem
        {
            private TimelineItem(QueryPersonnelTimelineItem item, bool hidePrivateNotes)
            {
                Id = item.Id;
                Workload = item.Workload;
                Type = item.Type;
                Description = item.Description;

                RoleName = hidePrivateNotes && item.IsNotePrivate == true ? "Not disclosed" : item.RoleName;
                TaskName = hidePrivateNotes && item.IsNotePrivate == true ? "Not disclosed" : item.TaskName;
                Location = hidePrivateNotes && item.IsNotePrivate == true ? "Not disclosed" : item.Location;

                if (item.Project != null) Project = new ApiProjectReference(item.Project);
                if (item.BasePosition != null) BasePosition = new ApiBasePosition(item.BasePosition);
            }

            public static TimelineItem CreateWithoutConfidentialTaskInfo(QueryPersonnelTimelineItem item) => new TimelineItem(item, hidePrivateNotes: true);
            public static TimelineItem CreateWithConfidentialTaskInfo(QueryPersonnelTimelineItem item) => new TimelineItem(item, hidePrivateNotes: false);

            public Guid Id { get; set; }
            public string Type { get; set; } = null!;
            public double? Workload { get; set; }
            public string Description { get; set; } = null!;

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public ApiProjectReference? Project { get; set; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public ApiBasePosition? BasePosition { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? RoleName { get; set; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? TaskName { get; set; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? Location { get; set; }
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

                HasChangeRequest = pos.HasChangeRequest;
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

            public bool HasChangeRequest { get; set; }
        }
    }

}
