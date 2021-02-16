using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class QueryDepartmentPersonnelPerson
    {
        public QueryDepartmentPersonnelPerson(Guid azureId, string? mail, string name, string accountType)
        {
            AzureUniqueId = azureId;
            Name = name;
            Mail = mail;
            AccountType = accountType;

            PositionInstances = new List<PersonnelPosition>();
            Absence = new List<QueryPersonAbsenceBasic>();
        }

        /// <summary>
        /// Internal personnel will always have unique id, as they must have an account.
        /// </summary>
        public Guid AzureUniqueId { get; set; }
        public string Name { get; set; } = null!;
        public string? JobTitle { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Mail { get; set; }
        public string? OfficeLocation { get; set; }

        /// <summary>
        /// Enum, <see cref="FusionAccountType"/>.
        /// </summary>
        public string AccountType { get; set; }

        public List<QueryTimelineRange<PersonnelTimelineItem>>? Timeline { get; set; }
        public List<PersonnelPosition> PositionInstances { get; set; }
        public List<QueryPersonAbsenceBasic> Absence { get; set; }


        public class PersonnelTimelineItem
        {
            public Guid Id { get; set; }
            public string Type { get; set; } = null!;
            public double? Workload { get; set; }
            public string Description { get; set; } = null!;

            public QueryProjectRef? Project { get; set; }
            public QueryBasePosition? BasePosition { get; set; }
        }


        public class PersonnelPosition
        {
            public Guid PositionId { get; set; }
            public Guid InstanceId { get; set; }
            public DateTime AppliesFrom { get; set; }
            public DateTime AppliesTo { get; set; }

            public QueryBasePosition BasePosition { get; set; } = null!;
            public string Name { get; set; } = null!;
            public string? Location { get; set; }

            public bool IsActive => AppliesFrom >= DateTime.UtcNow.Date && AppliesTo >= DateTime.UtcNow.Date;
            public double Workload { get; set; }
            public QueryProjectRef Project { get; set; } = null!;
        }
    }
    public class QueryTimelineRange<TItem>
    {
        public QueryTimelineRange(DateTime from, DateTime to)
        {
            AppliesFrom = from;
            AppliesTo = to;
        }
        public DateTime AppliesFrom { get; set; }
        public DateTime AppliesTo { get; set; }
        public List<TItem> Items { get; set; } = new List<TItem>();
        public double Workload { get; set; }
    }


}
