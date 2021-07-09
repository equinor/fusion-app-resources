using FluentValidation;

namespace Fusion.Resources.Api.Controllers
{
    public class UpdateRequestConversationMessageRequest
    {
        public string Title { get; set; } = null!;
        public string Body { get; set; } = null!;
        public string Category { get; set; } = null!;
        public ApiMessageRecipient Recipient { get; set; }
        public ApiPropertiesCollection? Properties { get; set; }

        public class Validator : AbstractValidator<AddRequestConversationMessageRequest>
        {
            public Validator()
            {
                RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
                RuleFor(x => x.Body).NotEmpty().MaximumLength(2000);
                RuleFor(x => x.Category).NotEmpty().MaximumLength(60);
            }
        }
    }
}
