using Fusion.ApiClients.Org;
using Fusion.Resources.Database.Entities;
using System;

#nullable enable

namespace Fusion.Resources.Domain
{
    public class QueryPositionRequest
    {
        public QueryPositionRequest(DbContractorRequest.RequestPosition position)
        {
            Name = position.Name;
            AppliesFrom = position.AppliesFrom;
            AppliesTo = position.AppliesTo;
            Workload = position.Workload;
            Obs = position.Obs;

            BasePosition = new QueryBasePosition(position.BasePositionId);

            TaskOwnerPositionId = position.TaskOwner?.PositionId;
        }

        public QueryBasePosition BasePosition { get; set; }
        public string Name { get; set; }
        public string? Obs { get; set; }
        public DateTime AppliesFrom { get; set; }
        public DateTime AppliesTo { get; set; }
        public double Workload { get; set; }

        public Guid? TaskOwnerPositionId { get; set; }

        public QueryPositionRequest WithResolvedBasePosition(ApiBasePositionV2? basePosition = null)
        {
            // Indicate failure unless not null. 
            BasePosition.Resolved = false;

            if (basePosition != null)
            {
                BasePosition = new QueryBasePosition(basePosition);
            }

            return this;
        }
    }
}