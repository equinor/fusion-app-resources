using System;

namespace Fusion.Resources.Authorization.Requirements
{
    public class TaskOwnerForPositionRequirement : Fusion.Authorization.FusionAuthorizationRequirement
    {
        public TaskOwnerForPositionRequirement(Guid orgProjectId, Guid orgPositionId, Guid orgPositionInstanceId)
        {
            OrgProjectId = orgProjectId;
            OrgPositionId = orgPositionId;
            OrgPositionInstanceId = orgPositionInstanceId;
        }

        public Guid OrgProjectId { get; }
        public Guid OrgPositionId { get; }
        public Guid OrgPositionInstanceId { get; }

        public override string Description => "Current user is task owner for the position";

        public override string Code => "TaskOwnerForPosition";
    }
}
