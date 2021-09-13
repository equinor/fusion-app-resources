using FluentValidation;
using Fusion.AspNetCore.Api;
using Fusion.Resources.Domain;
using System;

namespace Fusion.Resources.Api.Controllers
{
    public class PatchActionRequest : PatchRequest
    {
        public PatchProperty<string> Title { get; set; } = new();
        public PatchProperty<string> Body { get; set; } = new();
        public PatchProperty<string> Type { get; set; } = new();
        public PatchProperty<string?> SubType { get; set; } = new();
        public PatchProperty<bool> IsResolved { get; set; } = new();
        public PatchProperty<bool> IsRequired { get; set; } = new();
        public PatchProperty<DateTime?> DueDate { get; set; } = new();
        public PatchProperty<Guid?> AssignedToId { get; set; } = new();

        public PatchProperty<ApiPropertiesCollection> Properties { get; set; } = new();

        public class Validator : AbstractValidator<PatchActionRequest>
        {
            public Validator(IProfileService profileService)
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

                RuleFor(x => x.AssignedToId)
                    .MustAsync(async (assignedToId, cancelToken) =>
                    {
                        var assigned = await profileService.EnsurePersonAsync(assignedToId.Value!.Value);
                        return assigned != null;
                    })
                    .When(x => x.AssignedToId.HasValue && x.AssignedToId.Value.HasValue);
            }
        }
    }
}
