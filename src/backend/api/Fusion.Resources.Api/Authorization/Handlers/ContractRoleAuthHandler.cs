using Fusion.Integration.Org;
using Fusion.Resources.Domain;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;


namespace Fusion.Resources.Api.Authorization.Handlers
{
    internal class ContractRoleAuthHandler : AuthorizationHandler<ContractRole, ContractResource>
    {
        private readonly IProjectOrgResolver orgResolver;

        public ContractRoleAuthHandler(IProjectOrgResolver orgResolver)
        {
            this.orgResolver = orgResolver;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ContractRole requirement, ContractResource resource)
        {
            var contract = await orgResolver.ResolveContractAsync(resource.Project.ProjectId, resource.Contract);
            if (contract == null)
            {
                requirement.SetEvaluation($"Couldn't locate contract in project '{resource.Project.Name}'");
                return;
            }


            Guid userId = context.User.GetAzureUniqueIdOrThrow();

            bool isCompRep = contract.CompanyRep.HasActiveAssignment(userId);
            bool isContrResp = contract.ContractRep.HasActiveAssignment(userId);
            bool isInternal = isCompRep || isContrResp;
            bool isExternalCompRep = contract.ExternalCompanyRep.HasActiveAssignment(userId);
            bool isExternalContrResp = contract.ExternalContractRep.HasActiveAssignment(userId);
            bool isExternal = isExternalCompRep || isExternalContrResp;
            bool isAnyRole = isInternal || isExternal;


            if (!isAnyRole)
            {
                requirement.SetEvaluation("User does not have any role in the contract");
                return;
            }

            if (requirement.Classification == ContractRole.RoleClassification.Internal && !isInternal)
            {
                requirement.SetEvaluation("Internal company rep or contract responsible is required.");
                return;
            }

            if (requirement.Classification == ContractRole.RoleClassification.External && !isExternal)
            {
                requirement.SetEvaluation("External company rep or contract responsible is required.");
                return;
            }

            switch (requirement.Type)
            {
                case ContractRole.RoleType.Any:
                    if (isAnyRole)
                        context.Succeed(requirement);
                    break;

                case ContractRole.RoleType.CompanyRep:
                    if (isCompRep || isExternalCompRep)
                        context.Succeed(requirement);
                    break;

                case ContractRole.RoleType.ContractResponsible:
                    if (isContrResp || isExternalContrResp)
                        context.Succeed(requirement);
                    break;
            }

            requirement.SetEvaluation("User does not have any role on the contract.");

        }

    }
}
