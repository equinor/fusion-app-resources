using Fusion.Authorization;
using Fusion.Resources.Domain;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Authorization
{
    public class RequestCommentAuthor : FusionAuthorizationRequirement, IAuthorizationHandler
    {
        public override string Description => "The user needs to be the author of the comment";

        public override string Code => "CommentAuthor";

        public Task HandleAsync(AuthorizationHandlerContext context)
        {
            var azureId = context.User.GetAzureUniqueId();

            var resource = context.Resource as QueryRequestComment;

            if (resource?.CreatedBy.AzureUniqueId == azureId)
                context.Succeed(this);

            SetEvaluation("User is not author of the comment");

            return Task.CompletedTask;
        }
    }
}
