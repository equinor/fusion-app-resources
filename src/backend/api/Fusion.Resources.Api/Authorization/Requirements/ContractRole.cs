//using Fusion.AspNetCore.FluentAuthorization;
//using Fusion.Authorization;
//using System;

//namespace Fusion.Resources.Api.Authorization
//{
//    public class ContractRole : FusionAuthorizationRequirement
//    {
//        public ContractRole(RoleType type, RoleClassification? classification = null)
//        {
//            Type = type;
//            Classification = classification;
//        }

//        public override string Description => Type switch
//        {
//            RoleType.Any => Classification switch
//            {
//                RoleClassification.Internal => "The user must be in active assignment for any of the Equinor company rep/contract responsible positions.",
//                RoleClassification.External => "The user must be in active assignment for any of the contractor company rep/contract responsible positions.",
//                _ => "The user must be in active assignment for any of the Equinor or contractor company rep/contract responsible positions."
//            },
//            RoleType.CompanyRep => Classification switch
//            {
//                RoleClassification.Internal => "The user must be in active assignment for the Equinor company rep positions.",
//                RoleClassification.External => "The user must be in active assignment for the contractor company rep positions.",
//                _ => "The user must be in active assignment for either the Equinor or contractor company rep positions."
//            },
//            RoleType.ContractResponsible => Classification switch
//            {
//                RoleClassification.Internal => "The user must be in active assignment for the Equinor contract responsible positions.",
//                RoleClassification.External => "The user must be in active assignment for the contractor contract responsible positions.",
//                _ => "The user must be in active assignment for either the Equinor or contractor contract responsible positions."
//            },
//            _ => throw new NotSupportedException("Invalid role type to generate description for")
//        };

//        public override string Code => "ContractRepRole";



//        public RoleType Type { get; set; }
//        public RoleClassification? Classification { get; set; }


//        public static ContractRole AnyExternalRole = new ContractRole(RoleType.Any, RoleClassification.External);
//        public static ContractRole AnyInternalRole = new ContractRole(RoleType.Any, RoleClassification.Internal);
//        public static ContractRole Any = new ContractRole(RoleType.Any);


//        public enum RoleType { Any, CompanyRep, ContractResponsible }
//        public enum RoleClassification { Internal, External }

//    }
//}
