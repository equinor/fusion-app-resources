using Fusion.Integration;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using Fusion.Authorization;

namespace Fusion.Resources.Api.Authorization.Handlers
{
    internal class ContractorInContractHandler : AuthorizationHandler<ContractorInContractRequirement>
    {

        public ContractorInContractHandler()
        {
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ContractorInContractRequirement requirement)
        {
            var contractContractIds = context.User.Claims.Where(c => c.Type == FusionClaimsTypes.FusionContract && c.Properties.ContainsKey(FusionClaimsProperties.ContractId))
                        .Select(c => { Guid.TryParse(c.Properties[FusionClaimsProperties.ContractId], out Guid projectId); return projectId; });

            if (contractContractIds.Any(cid => cid == requirement.ContractId))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            requirement.SetEvaluation($"User does not have a position in the specified contract");
            return Task.CompletedTask;
        }
    }
}
