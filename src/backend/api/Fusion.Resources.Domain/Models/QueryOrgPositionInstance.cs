using System;
using Fusion.Integration.Profile;
using Fusion.Resources.Domain.Services;
using Fusion.Resources.Domain.Services.OrgClient.Models;
using Fusion.Services.Org.ApiModels;

namespace Fusion.Resources.Domain
{
    public class QueryOrgPositionInstance
    {
        public QueryOrgPositionInstance(FusionContract contract, FusionContractPosition positionInstance)
        {
            Project = new ApiProjectReference
            {
                DomainId = contract.Project.DomainId,
                Name = contract.Project.Name,
                ProjectType = contract.Project.Type,
                ProjectId = contract.Project.Id
            };
            Contract = new ApiContractReferenceV2
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

        public ApiProjectReference Project { get; set; }
        public ApiContractReferenceV2 Contract { get; set; }
    }
}
