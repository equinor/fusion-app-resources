using System;
using Fusion.Integration.Profile;

namespace Fusion.Resources.Domain
{
    public class QueryOrgPositionInstance
    {
        public QueryOrgPositionInstance(FusionContract contract, FusionContractPosition positionInstance)
        {
            Project = new ApiClients.Org.ApiProjectReferenceV2
            {
                DomainId = contract.Project.DomainId,
                Name = contract.Project.Name,
                ProjectType = contract.Project.Type,
                ProjectId = contract.Project.Id
            };
            Contract = new ApiClients.Org.ApiContractReferenceV2
            {
                Name = contract.Name,
                Id = contract.Id,
                ContractNumber = contract.ContractNumber
                // TODO, company
            };

            Id = positionInstance.Id;
            PositionId = positionInstance.PositionId;
            Name = positionInstance.Name;
            Obs = positionInstance.Obs;
            ExternalPositionId = positionInstance.ExternalPositionId;
            AppliesFrom = positionInstance.AppliesFrom;
            AppliesTo = positionInstance.AppliesTo;
            Workload = positionInstance.Workload;
            BasePosition = new QueryBasePosition(positionInstance.BasePosition);
        }

        public Guid PositionId { get; set; }

        /// <summary>
        /// Id of the instance the person is assigned to.
        /// </summary>
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? Obs { get; set; }
        public string? ExternalPositionId { get; set; }
        public DateTime? AppliesFrom { get; set; }
        public DateTime? AppliesTo { get; set; }
        public double? Workload { get; set; }
        public QueryBasePosition BasePosition { get; set; }

        public Fusion.ApiClients.Org.ApiProjectReferenceV2 Project { get; set; }
        public Fusion.ApiClients.Org.ApiContractReferenceV2 Contract { get; set; }
    }
}
