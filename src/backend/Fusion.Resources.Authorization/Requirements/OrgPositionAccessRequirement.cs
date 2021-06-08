using System;

namespace Fusion.Resources.Api.Authorization.Requirements
{
    public class OrgPositionAccessRequirement : Fusion.Authorization.FusionAuthorizationRequirement
    {
        public Guid OrgProjectId { get; }
        public Guid OrgPositionId { get; }
        public AccessLevel RequiredLevel { get; }

        public override string Description => $"Must atleast have '{RequiredLevel}' access on the specific position in the org chart";

        public override string Code => "OrgChartAccess";

        public enum AccessLevel { Read, Write }

        private OrgPositionAccessRequirement(AccessLevel requiredLevel, Guid orgProjectId, Guid orgPositionId)
        {
            RequiredLevel = requiredLevel;
            OrgProjectId = orgProjectId;
            OrgPositionId = orgPositionId;
        }

        public static OrgPositionAccessRequirement OrgPositionRead(Guid orgProjectId, Guid orgPositionId) => new OrgPositionAccessRequirement(AccessLevel.Read, orgProjectId, orgPositionId);
        public static OrgPositionAccessRequirement OrgPositionWrite(Guid orgProjectId, Guid orgPositionId) => new OrgPositionAccessRequirement(AccessLevel.Write, orgProjectId, orgPositionId);

    }
}
