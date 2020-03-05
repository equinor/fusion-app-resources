using Fusion.Resources.Database.Entities;
using System;

namespace Fusion.Resources.Domain
{
    public class QueryContract
    {
        public QueryContract(DbContract contract)
        {
            Id = contract.Id;
            Name = contract.Name;
            ContractNumber = contract.ContractNumber;
            OrgContractId = contract.OrgContractId;
        }

        public Guid Id { get; set; }
        public string Name { get; set; }
        public string ContractNumber { get; set; }
        public Guid OrgContractId { get; set; }
    }
}
