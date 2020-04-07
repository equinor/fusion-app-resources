using FluentValidation;

namespace Fusion.Resources.Api.Controllers
{
    public class CreateRequestComment
    {
        public string Comment { get; set; } = null!;

        public class Validator : AbstractValidator<CreateRequestComment>
        {
            public Validator()
            {
                RuleFor(c => c.Comment).NotEmpty().WithMessage("Cannot add comment without content");
                RuleFor(c => c.Comment).NotContainScriptTag().WithMessage("Detected dangerous content in comment");
            }
        }
    }
}
