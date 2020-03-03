using Fusion.ApiClients.Org;
using System;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiContract
    {
        public ApiContract() { }
        public ApiContract(ApiProjectContractV2 orgContract)
        {
            Id = orgContract.Id;
            ContractNumber = orgContract.ContractNumber;
            Name = orgContract.Name;
            Description = orgContract.Description;

            if (orgContract.Company != null)
                Company = new ApiCompany { Id = orgContract.Company.Id, Name = orgContract.Company.Name };

            ContractResponsiblePositionId = orgContract.ContractRep?.Id;
            CompanyRepPositionId = orgContract.CompanyRep?.Id;

            ExternalCompanyRepPositionId = orgContract.ExternalCompanyRep?.Id;
            ExternalContractResponsiblePositionId = orgContract.ExternalContractRep?.Id;

            ContractResponsible = orgContract.ContractRep;
            CompanyRep = orgContract.CompanyRep;
            ExternalCompanyRep = orgContract.ExternalCompanyRep;
            ExternalContractResponsible = orgContract.ExternalContractRep;
        }

        public Guid Id { get; set; }
        public string ContractNumber { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public ApiCompany? Company { get; set; }
        
        public Guid? ContractResponsiblePositionId { get; set; }
        public Guid? CompanyRepPositionId { get; set; }
        public Guid? ExternalContractResponsiblePositionId { get; set; }
        public Guid? ExternalCompanyRepPositionId { get; set; }

        #region Read only props
        public ApiPositionV2? ContractResponsible { get; set; }
        public ApiPositionV2? CompanyRep { get; set; }

        public ApiPositionV2? ExternalContractResponsible { get; set; }
        public ApiPositionV2? ExternalCompanyRep { get; set; }
        #endregion
    }
}
