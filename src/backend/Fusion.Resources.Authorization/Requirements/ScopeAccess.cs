using Fusion.Authorization;

namespace Fusion.Resources.Api.Authorization
{
    public class ScopeAccess : FusionAuthorizationRequirement
    {
        private readonly string code;
        private readonly string description;

        private ScopeAccess(AccessType type, string code, string description)
        {
            Type = type;
            this.code = code;
            this.description = description;
        }

        public AccessType Type { get; }

        public override string Description => description;

        public override string Code => code;


        public static readonly ScopeAccess ManageMatrices = new ScopeAccess(AccessType.ManageMatrix, "Fusion.Resources.Internal.ResponsibilityMatrix",
            "You need permission to manage matrix. This is typically granted to persons responsible.");

        public static readonly ScopeAccess QueryAnalyticsRequests = new ScopeAccess(AccessType.AnalyticsRequests, "Fusion.Analytics.Requests", "You need permission to query requests.");


        public enum AccessType { ManageMatrix, AnalyticsRequests }
    }
}
