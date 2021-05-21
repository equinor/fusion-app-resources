using System;

namespace Fusion.Resources.Api.Authorization.Requirements
{
    public class OrgProjectAccessRequirement : Fusion.Authorization.FusionAuthorizationRequirement
    {
        public Guid OrgProjectId { get; }
        public AccessLevel RequiredLevel { get; }

        public override string Description => $"Must atleast have '{RequiredLevel}' access on the specific org chart";

        public override string Code => "OrgChartAccess";

        public enum AccessLevel { Read, Write }

        private OrgProjectAccessRequirement(AccessLevel requiredLevel, Guid orgProjectId)
        {
            RequiredLevel = requiredLevel;
            OrgProjectId = orgProjectId;
        }

        public static OrgProjectAccessRequirement OrgRead(Guid orgProjectId) => new OrgProjectAccessRequirement(AccessLevel.Read, orgProjectId);
        public static OrgProjectAccessRequirement OrgWrite(Guid orgProjectId) => new OrgProjectAccessRequirement(AccessLevel.Write, orgProjectId);

    }
}
