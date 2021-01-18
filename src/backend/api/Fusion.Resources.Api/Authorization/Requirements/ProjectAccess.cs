using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Authorization;

namespace Fusion.Resources.Api.Authorization
{
    public class ProjectAccess : FusionAuthorizationRequirement
    {
        private readonly string code;
        private readonly string description;

        private ProjectAccess(AccessType type, string code, string description)
        {
            Type = type;
            this.code = code;
            this.description = description;
        }

        public AccessType Type { get; }

        public override string Description => description;

        public override string Code => code;


        public static ProjectAccess ManageContracts = new ProjectAccess(AccessType.ManageContracts, "Project.Contract.Manage",
            "You need permission to manage contracts in context of the project. This is typically granted to procurement personnel.");

        public static ProjectAccess ManageRequests = new ProjectAccess(AccessType.ManageRequests, "Project.Request.Manage",
            "You need permission to manage requests in context of the project. This is typically granted to procurement personnel.");

        public enum AccessType { ManageContracts, ManageRequests }
    }
}
