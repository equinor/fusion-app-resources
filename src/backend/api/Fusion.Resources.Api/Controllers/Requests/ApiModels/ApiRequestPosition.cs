using Fusion.Resources.Domain;
using System;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiRequestPosition
    {
        public ApiRequestPosition(QueryPositionRequest position)
        {

            Name = position.Name;
            AppliesFrom = position.AppliesFrom;
            AppliesTo = position.AppliesTo;
            Workload = position.Workload;

            if (position.TaskOwnerPositionId.HasValue)
                TaskOwner = new ApiRequestTaskOwner { PositionId = position.TaskOwnerPositionId.Value };

            BasePosition = new ApiRequestBasePosition(position.BasePosition);
        }

        /// <summary>
        /// The actual position id in the org chart..
        /// </summary>
        public Guid? Id { get; set; }
        public string? ExternalId { get; set; }

        public string Name { get; set; }
        public DateTime AppliesFrom { get; set; }
        public DateTime AppliesTo { get; set; }
        public double Workload { get; set; }

        public ApiRequestBasePosition BasePosition { get; set; }
        public ApiRequestTaskOwner? TaskOwner { get; set; }
    }
}
