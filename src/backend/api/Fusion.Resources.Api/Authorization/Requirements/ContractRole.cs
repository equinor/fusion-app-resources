using Fusion.AspNetCore.FluentAuthorization;

namespace Fusion.Resources.Api.Authorization
{
    public class ContractRole : IReportableAuthorizationRequirement
    {
        public ContractRole(RoleType type, RoleClassification? classification = null)
        {
            Type = type;
            Classification = classification;
        }

        public string Description { get; set; } = string.Empty;

        public string? Evaluation { get; set; }

        public string Code { get; set; } = "ContractRole";

        public bool IsEvaluated { get; set; }


        public RoleType Type { get; set; }
        public RoleClassification? Classification { get; set; }


        public static ContractRole AnyExternalRole = new ContractRole(RoleType.Any, RoleClassification.External);
        public static ContractRole AnyInternalRole = new ContractRole(RoleType.Any, RoleClassification.Internal);
        public static ContractRole Any = new ContractRole(RoleType.Any);


        public enum RoleType { Any, CompanyRep, ContractResponsible }
        public enum RoleClassification { Internal, External }

        public void SetFailure(string message)
        {
            IsEvaluated = true;
            Evaluation = message;
        }

    }
}
