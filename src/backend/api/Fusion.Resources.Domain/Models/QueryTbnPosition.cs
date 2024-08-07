using System;
using Fusion.ApiClients.Org;

namespace Fusion.Resources.Domain.Queries
{
    public class QueryTbnPosition
    {
        public QueryTbnPosition(ApiPositionV2 pos, ApiPositionInstanceV2 instance, string? projectState)
        {
            PositionId = pos.Id;
            InstanceId = instance.Id;
            ParentPositionId = pos.ExternalId;
            ProjectId = pos.ProjectId;

            Project = new QueryProjectRef(pos.Project.ProjectId, pos.Project.Name, pos.Project.DomainId,
                pos.Project.ProjectType, projectState);

            BasePosition = pos.BasePosition;
            Name = pos.Name;

            AppliesFrom = instance.AppliesFrom.Date;
            AppliesTo = instance.AppliesTo.Date;

            Workload = instance.Workload;
            Obs = instance.Obs;
        }

        public Guid PositionId { get; set; }
        public Guid InstanceId { get; set; }
        public string? ParentPositionId { get; set; }

        public string Name { get; set; } = null!;
        public Guid ProjectId { get; set; }
        public ApiPositionBasePositionV2 BasePosition { get; set; } = null!;

        public DateTime AppliesTo { get; set; }
        public DateTime AppliesFrom { get; set; }
        public double? Workload { get; set; }
        public string? Obs { get; set; }
        public QueryProjectRef Project { get; internal set; }
    }
}
