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
            var userId = context.User.GetAzureUniqueIdOrThrow();
            var activeTaskManagers = reportingPath
                .Where(x => x.IsTaskOwner)
                .SelectMany(x => x.Instances)
                .Where(x => x.AppliesFrom <= DateTime.UtcNow && DateTime.UtcNow <= x.AppliesTo)
                .Select(x => x.AssignedPerson);

            foreach (var person in activeTaskManagers)
            {
                if(person.AzureUniqueId == userId)
                {
                    context.Succeed(requirement);
                    return;
                }
            }
             
        }
    }
}
