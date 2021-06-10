using Fusion.Resources.Authorization.Requirements;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Fusion.Resources.Authorization.Handlers
{
    public class TaskOwnerForPositionAuthHandler : AuthorizationHandler<TaskOwnerForPositionRequirement>
    {
        private IOrgApiClient orgClient;

        public TaskOwnerForPositionAuthHandler(IOrgApiClientFactory apiClientFactory)
        {
            orgClient = apiClientFactory.CreateClient(ApiClientMode.Application);
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, TaskOwnerForPositionRequirement requirement)
        {
            var reportingPath = await orgClient.GetReportingPath(requirement.OrgProjectId, requirement.OrgPositionId, requirement.OrgPositionInstanceId);
            var userId = context.User.GetAzureUniqueId();

            if (userId is null) return;

            var activeTaskManagers = reportingPath
                .Where(x => x.IsTaskOwner)
                .SelectMany(x => x.Instances)
                .Where(x => x.AppliesFrom.Date <= DateTime.UtcNow.Date && DateTime.UtcNow.Date <= x.AppliesTo.Date)
                .Where(x => x.AssignedPerson != null)
                .Select(x => x.AssignedPerson);

            if (activeTaskManagers.Any(x => x.AzureUniqueId == userId))
            {
                context.Succeed(requirement);
                return;
            }
            requirement.SetEvaluation("User is not a task owner for the position.");
        }
    }
}
