using System;
using Fusion.Resources.Domain;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiProposalParameters
    {
        public ApiProposalParameters(QueryResourceAllocationRequest.QueryPropsalParameters proposalParameters)
        {
            ChangeDateFrom = proposalParameters.ChangeFrom;
            ChangeDateTo = proposalParameters.ChangeTo;

            Scope = proposalParameters.Scope;
            Type = proposalParameters.ChangeType;
        }

        public DateTime? ChangeDateFrom { get; set; }
        public DateTime? ChangeDateTo { get; set; }

        public string Scope { get; set; }
        public string? Type { get; set; }
    }
}
