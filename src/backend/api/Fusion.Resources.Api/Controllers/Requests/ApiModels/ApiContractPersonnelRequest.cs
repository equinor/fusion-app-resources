using System;
using System.Collections.Generic;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiContractPersonnelRequest
    {
        public Guid Id { get; set; }

        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
        public ApiPerson CreatedBy { get; set; }
        public ApiPerson UpdatedBy { get; set; }
        public ApiRequestState State { get; set; }
        public string Description { get; set; }

        public ApiRequestPosition Position { get; set; }
        public ApiContractPersonnel Person { get; set; }

        public ApiContractReference Contract { get; set; }
        public ApiProjectReference Project { get; set; }

        public List<ApiRequestComment> Comments { get; set; }

        public enum ApiRequestState { Created, Submitted, Approved, Rejected, Provisioned }
    }

}
