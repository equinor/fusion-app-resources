using Fusion.Resources.Domain;
using System;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiContractReference
    {
        public ApiContractReference()
        {
        }
        public ApiContractReference(QueryContract contract)
        {
            Id = contract.OrgContractId;
            InternalId = contract.Id;
            ContractNumber = contract.ContractNumber;
            Name = contract.Name;
        }

        public Guid Id { get; set; }
        public Guid InternalId { get; set; }
        public string ContractNumber { get; set; }
        public string Name { get; set; }

        // Not sure if we want/need this? Could perhaps add an outgoing enrichment layer to fetch stuff from org chart.
        public ApiCompany? Company { get; set; } = null!;
    }
}
