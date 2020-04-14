using FluentValidation;

namespace Fusion.Resources.Api.Controllers
{
    public class RequestCommentRequest
    {
        public string Content { get; set; } = null!;

        public class Validator : AbstractValidator<RequestCommentRequest>
        {
            public Validator()
            {
                RuleFor(c => c.Content).NotEmpty().WithMessage("Cannot add comment without content");
                RuleFor(c => c.Content).NotContainScriptTag().WithMessage("Detected dangerous content in comment");
            }
        }
    }
}
