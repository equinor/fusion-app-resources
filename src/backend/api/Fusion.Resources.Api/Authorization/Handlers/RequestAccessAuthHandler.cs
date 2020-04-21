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
        private readonly IMediator mediator;

        public RequestAccessAuthHandler(IMediator mediator)
        {
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
            var hasAccess = await mediator.Send(new ContractorPersonnelRequest.CheckUserAccess(resource.Id));

            if (hasAccess)
            {
                context.Succeed(requirement);
            }

            requirement.SetEvaluation("No access to the request workflow");
        }
        
    }
}
