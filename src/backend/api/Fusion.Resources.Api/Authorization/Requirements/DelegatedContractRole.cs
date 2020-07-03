using Fusion.Authorization;
using System;

namespace Fusion.Resources.Api.Authorization
{
    public class DelegatedContractRole : FusionAuthorizationRequirement
    {
        public DelegatedContractRole(RoleType type, RoleClassification? classification = null)
        {
            Type = type;
            Classification = classification;
        }

        public override string Description => Type switch
        {
            RoleType.Any => Classification switch
            {
                RoleClassification.Internal => "The user must have been delegated any 'internal' admin role in the contract. This can be done by the internal company rep.",
                RoleClassification.External => "The user must have been delegated any 'external' admin role in the contract. This can be done by any of the Company reps (internal/external).",
                _ => throw new NotSupportedException("Unsupported role classification")
            },
            RoleType.CompanyRep => Classification switch
            {
                RoleClassification.Internal => "The user must be delegated the CR role for Equinor.",
                RoleClassification.External => "The user must be delegated the CR role for the external contractor",
                _ => throw new NotSupportedException("Unsupported role classification")
            },
            _ => throw new NotSupportedException("Invalid role type to generate description for")
        };

        public override string Code => "DelegatedAdminRole";



        public RoleType Type { get; set; }
        public RoleClassification? Classification { get; set; }


        public static DelegatedContractRole AnyExternalRole = new DelegatedContractRole(RoleType.Any, RoleClassification.External);
        public static DelegatedContractRole AnyInternalRole = new DelegatedContractRole(RoleType.Any, RoleClassification.Internal);
        public static DelegatedContractRole Any = new DelegatedContractRole(RoleType.Any);


        public enum RoleType { Any, CompanyRep }
        public enum RoleClassification { Internal, External }

    }
}
