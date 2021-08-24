using FluentValidation;
using Fusion.AspNetCore.Api;

namespace Fusion.Resources.Api.Controllers
{
    public class PatchActionRequest : PatchRequest
    {
        public PatchProperty<string> Title { get; set; } = new();
        public PatchProperty<string> Body { get; set; } = new();
        public PatchProperty<string> Type { get; set; } = new();
        public PatchProperty<string?> SubType { get; set; } = new();
        public PatchProperty<bool> IsResolved { get; set; } = new();
        public PatchProperty<ApiPropertiesCollection> Properties { get; set; } = new();

        public class Validator : AbstractValidator<PatchActionRequest>
        {
            public Validator()
            {
                RuleFor(x => x.Title.Value)
                    .MaximumLength(100)
                    .When(x => x.Title.HasValue);
                
                RuleFor(x => x.Body.Value)
                    .MaximumLength(2000)
                    .When(x => x.Body.HasValue);

                RuleFor(x => x.Type.Value)
                    .MaximumLength(60)
                    .When(x => x.Type.HasValue);
                
                RuleFor(x => x.SubType.Value)
                    .MaximumLength(60)
                    .When(x => x.SubType.HasValue && !string.IsNullOrEmpty(x.SubType.Value));
            }
        }
    }
}
