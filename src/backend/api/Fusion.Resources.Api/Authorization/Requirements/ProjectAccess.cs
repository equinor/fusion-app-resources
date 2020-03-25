using Fusion.AspNetCore.FluentAuthorization;

namespace Fusion.Resources.Api.Authorization
{
    public class ProjectAccess : IReportableAuthorizationRequirement
    {
        private ProjectAccess(AccessType type)
        {
            Type = type;
        }

        public AccessType Type { get; }

        public string Description { get; private set; } = string.Empty;

        public string? Evaluation { get; set; }

        public string Code { get; set; } = "ProjectAccess";

        public bool IsEvaluated { get; set; }



        public static ProjectAccess ManageContracts = new ProjectAccess(AccessType.ManageContracts)
        {
            Code = "Project.Contract.Manage",
            Description = "You need permission to manage contracts in context of the project. This is typically granted to procurement personnel."
        };


        public enum AccessType { ManageContracts }
    }
}
