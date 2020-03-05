using System;

namespace Fusion.Resources.Api.Controllers
{
    public class ContractRequest
    {
        public Guid? Id { get; set; }
        public string ContractNumber { get; set; } = null!;
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public CompanyReference? Company { get; set; }

        public Guid? ContractResponsiblePositionId { get; set; }
        public Guid? CompanyRepPositionId { get; set; }
        public Guid? ExternalContractResponsiblePositionId { get; set; }
        public Guid? ExternalCompanyRepPositionId { get; set; }
    }
}
