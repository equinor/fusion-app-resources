using Fusion.Integration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Authorization.Handlers
{
    internal class ProjectAccessAuthHandler : AuthorizationHandler<ProjectAccess, Controllers.ProjectIdentifier>
    {
        private readonly IFusionProfileResolver profileResolver;

        public ProjectAccessAuthHandler(IFusionProfileResolver profileResolver)
        {
            this.profileResolver = profileResolver;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ProjectAccess requirement, Controllers.ProjectIdentifier resource)
        {
            switch (requirement.Type)
            {
                case ProjectAccess.AccessType.ManageContracts:
                    await VerifyManageContractsAsync(context, requirement, resource);
                    break;
            }
        }

        private async Task VerifyManageContractsAsync(AuthorizationHandlerContext context, ProjectAccess requirement, Controllers.ProjectIdentifier resource)
        {           
            // User must be employee

            if (context.User.GetUserAccountType() == Fusion.Integration.Profile.FusionAccountType.Employee)
            {
                // User must work in procurement position in the project
                var profile = await profileResolver.GetCurrentUserFullProfileAsync();

                var positions = profile.Positions.Where(p => p.Project.Id == resource.ProjectId);
                
                if (positions.Any(p => p.BasePosition.Discipline == "Procurement"))
                {
                    context.Succeed(requirement);
                }
            }

            requirement.SetEvaluation($"User does not have any procurement position in the project '{resource.Name}'");
        }
    }
}
