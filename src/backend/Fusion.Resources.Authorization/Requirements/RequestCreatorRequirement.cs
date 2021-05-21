using Fusion.Authorization;
using System;

namespace Fusion.Resources.Api.Authorization.Requirements
{
    public class RequestCreatorRequirement : FusionAuthorizationRequirement
    {
        public RequestCreatorRequirement(Guid requestId)
        {
            this.RequestId = requestId;
        }

        public override string Description => "The user must be the creator of the request.";
        public override string Code => "AllocationReqCreator";

        public Guid RequestId { get; }
    }
}
