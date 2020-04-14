using Fusion.Resources.Domain;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Authorization.Handlers
{
    public class RequestCommentAuthorHandler : AuthorizationHandler<RequestCommentAuthor, QueryRequestComment>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RequestCommentAuthor requirement, QueryRequestComment resource)
        {
            var azureId = context.User.GetAzureUniqueId();

            if (resource.CreatedBy.AzureUniqueId == azureId)
                context.Succeed(requirement);

            requirement.SetEvaluation("User is not author of the comment");

            return Task.CompletedTask;
        }
    }
}
