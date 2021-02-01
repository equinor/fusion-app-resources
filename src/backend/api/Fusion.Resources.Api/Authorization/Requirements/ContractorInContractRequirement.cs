//using Fusion.Authorization;
//using System;

//namespace Fusion.Resources.Api.Authorization
//{
//    public class ContractorInContractRequirement : FusionAuthorizationRequirement
//    {
//        public ContractorInContractRequirement(Guid contractId)
//        {
//            ContractId = contractId;
//        }

//        public Guid ContractId { get; }

//        public override string Description => $"User must be assigned a position in the contract with id '{ContractId}'";

//        public override string Code => "ContractorInContract";

//    }
//}
