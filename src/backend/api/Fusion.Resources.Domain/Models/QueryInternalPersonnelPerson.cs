using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class QueryInternalPersonnelPerson
    {
        public QueryInternalPersonnelPerson(Guid azureId, string? mail, string name, string accountType)
        {
            AzureUniqueId = azureId;
            Name = name;
            Mail = mail;
            AccountType = accountType;

            PositionInstances = new List<QueryPersonnelPosition>();
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
        public string? Department { get; set; }
        public string? FullDepartment { get; set; }

        /// <summary>
        /// Enum, <see cref="FusionAccountType"/>.
        /// </summary>
        public string AccountType { get; set; }

        public List<QueryTimelineRange<QueryPersonnelTimelineItem>>? Timeline { get; set; }
        public List<QueryPersonnelPosition> PositionInstances { get; set; }
        public List<QueryPersonAbsenceBasic> Absence { get; set; }
        public bool IsResourceOwner { get; set; }
        public Guid? ManagerAzureId { get; set; }
        public List<QueryResourceAllocationRequest> PendingRequests { get; set; } = new();
    }
}
