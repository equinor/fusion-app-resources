using Fusion.Resources.Authorization.Requirements;
using Microsoft.AspNetCore.Authorization;
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
            var taskOwnerResponse = await orgClient.GetInstanceTaskOwnerAsync(requirement.OrgProjectId, requirement.OrgPositionId, requirement.OrgPositionInstanceId);
            var userId = context.User.GetAzureUniqueIdOrThrow();

            foreach (var person in taskOwnerResponse.Value.Persons)
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
