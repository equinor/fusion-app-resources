using Fusion.Authorization;

namespace Fusion.Resources.Api.Authorization
{
    public class RequestCommentAuthor : FusionAuthorizationRequirement
    {
        public override string Description => "The user needs to be the author of the comment";

        public override string Code => "CommentAuthor";
    }
}
