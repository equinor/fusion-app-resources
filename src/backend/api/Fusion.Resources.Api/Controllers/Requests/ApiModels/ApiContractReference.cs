using System;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiContractReference
    {
        public Guid Id { get; set; }
        public string ContractNumber { get; set; }
        public string Name { get; set; }
        public ApiCompany Company { get; set; }
    }
}
