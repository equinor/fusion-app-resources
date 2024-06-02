using Fusion.Resources.Domain.Models;
using System;

namespace Fusion.Resources.Domain
{
    public class QueryPersonnelPosition
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
        public string? Obs { get; set; }
        public QueryProjectRef Project { get; set; } = null!;

        /// <summary>
        /// Indicates that the allocation has been changed outside approved channels.
        /// Null or "ChangedByTaskOwner". 
        /// </summary>
        public string? AllocationState { get; set; }
        public DateTime? AllocationUpdated { get; set; }

        public bool HasChangeRequest { get; set; }
        public QueryRequestStatus? ChangeRequestStatus { get; internal set; }
    }
}
