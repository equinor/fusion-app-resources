using Fusion.AspNetCore.FluentAuthorization;

namespace Fusion.Resources.Api.Authorization
{
    public class RequestAccess : IReportableAuthorizationRequirement
    {
        public RequestAccess(AccessType type)
        {
            Type = type;
        }

        public string Description { get; set; } = string.Empty;

        public string? Evaluation { get; set; }

        public string Code { get; set; } = "RequestAccess";

        public bool IsEvaluated { get; set; }


        public AccessType Type { get; set; }


        public static RequestAccess Workflow = new RequestAccess(AccessType.Workflow);


        public enum AccessType { Workflow }

        public void SetFailure(string message)
        {
            IsEvaluated = true;
            Evaluation = message;
        }

    }
}
