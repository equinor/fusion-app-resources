using Fusion.ApiClients.Org;
using Fusion.Resources.Domain;
using System;
using System.Linq;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiRequestPosition
    {
        private ApiRequestPosition(ApiPositionV2 orgChartPosition)
        {
            Id = orgChartPosition.Id;
            ExternalId = orgChartPosition.ExternalId;
            Name = orgChartPosition.Name;
            BasePosition = new ApiRequestBasePosition(orgChartPosition.BasePosition);

            var instance = orgChartPosition.Instances.FirstOrDefault();
            if (instance != null)
            {
                AppliesFrom = instance.AppliesFrom;
                AppliesTo = instance.AppliesTo;
                Workload = instance.Workload.GetValueOrDefault(0);
                Obs = instance.Obs;

                if (instance.ParentPositionId.HasValue)
                    TaskOwner = new ApiRequestTaskOwner { PositionId = instance.ParentPositionId.Value };
            }
        }
        public ApiRequestPosition(QueryPositionRequest position)
        {

            Name = position.Name;
            AppliesFrom = position.AppliesFrom;
            AppliesTo = position.AppliesTo;
            Workload = position.Workload;
            Obs = position.Obs;

            if (position.TaskOwnerPositionId.HasValue)
                TaskOwner = new ApiRequestTaskOwner { PositionId = position.TaskOwnerPositionId.Value };

            BasePosition = new ApiRequestBasePosition(position.BasePosition);
        }

        /// <summary>
        /// The actual position id in the org chart..
        /// </summary>
        public Guid? Id { get; set; }
        public string? ExternalId { get; set; }

        public string? Name { get; set; }
        public string? Obs { get; set; }
        public DateTime AppliesFrom { get; set; }
        public DateTime AppliesTo { get; set; }
        public double Workload { get; set; }

        public ApiRequestBasePosition BasePosition { get; set; }
        public ApiRequestTaskOwner? TaskOwner { get; set; }

        internal static ApiRequestPosition? FromEntityOrDefault(ApiPositionV2? resolvedOriginalPosition)
        {
            if (resolvedOriginalPosition == null)
                return null;

            if (resolvedOriginalPosition.Instances.Count > 1)
                return null;

            return new ApiRequestPosition(resolvedOriginalPosition);
        }
    }
}
