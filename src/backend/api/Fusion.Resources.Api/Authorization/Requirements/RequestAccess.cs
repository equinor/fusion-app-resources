using Fusion.Authorization;

namespace Fusion.Resources.Api.Authorization
{
    public class RequestAccess : FusionAuthorizationRequirement
    {
        private readonly string description;

        public RequestAccess(AccessType type, string description)
        {
            Type = type;
            this.description = description;
        }

        public override string Description => description;

        public override string Code => "WorkflowRequestAccess";

        
        public AccessType Type { get; set; }


        public static RequestAccess Workflow = new RequestAccess(AccessType.Workflow, 
            "The user needs access to the workflow process running on the personnel request.");


        public enum AccessType { Workflow }

    }
}
