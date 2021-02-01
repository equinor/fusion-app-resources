using Fusion.Integration;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Authorization.Handlers
{
    internal class ContractorInProjectHandler : AuthorizationHandler<ContractorInProjectRequirement, Controllers.ProjectIdentifier>
    {

        public ContractorInProjectHandler()
        {
        }

        protected override  Task HandleRequirementAsync(AuthorizationHandlerContext context, ContractorInProjectRequirement requirement, Controllers.ProjectIdentifier resource)
        {

            var contractProjectIds = context.User.Claims.Where(c => c.Type == FusionClaimsTypes.FusionContract && c.Properties.ContainsKey(FusionClaimsProperties.ProjectId))
                .Select(c => { Guid.TryParse(c.Properties[FusionClaimsProperties.ProjectId], out Guid projectId); return projectId; });

            if (contractProjectIds.Any(pid => pid == resource.ProjectId))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            requirement.SetEvaluation($"User is not a member of any contract attached to the project '{resource.Name}' ({resource.ProjectId})");

            return Task.CompletedTask;
        }

    }
}