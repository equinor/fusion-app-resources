using Fusion.Resources.Domain;
using Fusion.Resources.Logic.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Authorization.Handlers
{
    internal class RequestAccessAuthHandler : AuthorizationHandler<RequestAccess, QueryPersonnelRequest>
    {
        private readonly IProjectOrgResolver orgResolver;
        private readonly IMediator mediator;

        public RequestAccessAuthHandler(IProjectOrgResolver orgResolver, IMediator mediator)
        {
            this.orgResolver = orgResolver;
            this.mediator = mediator;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, RequestAccess requirement, QueryPersonnelRequest resource)
        {
            switch (requirement.Type)
            {
                case RequestAccess.AccessType.Workflow:
                    await EvaluateWorkflowAccessAsync(context, requirement, resource);
                    break;
            }
        }

        private async Task EvaluateWorkflowAccessAsync(AuthorizationHandlerContext context, RequestAccess requirement, QueryPersonnelRequest resource)
        {
            var hasNaturalAccess = await mediator.Send(new ContractorPersonnelRequest.CheckUserAccess(resource.Id));

            if (hasNaturalAccess)
            {
                context.Succeed(requirement);
            }

            requirement.SetFailure("No access to the request workflow");
        }
        
    }

}
