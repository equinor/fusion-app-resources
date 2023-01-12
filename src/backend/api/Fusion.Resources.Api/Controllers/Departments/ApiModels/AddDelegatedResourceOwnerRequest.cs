using FluentValidation;
using System;

namespace Fusion.Resources.Api.Controllers
{
    public class AddDelegatedResourceOwnerRequest
    {
        public Guid ResponsibleAzureUniqueId { get; set; }
        public DateTimeOffset DateTo { get; set; }
        public DateTimeOffset DateFrom { get; set; }
        public string? Reason { get; set; }

        public class Validator : AbstractValidator<AddDelegatedResourceOwnerRequest>
        {
            public Validator()
            {
                RuleFor(x => x.ResponsibleAzureUniqueId).NotEmpty();
                RuleFor(x => x.DateFrom).GreaterThanOrEqualTo(DateTimeOffset.UtcNow.AddDays(-1));
                RuleFor(x => x.DateTo).GreaterThanOrEqualTo(DateTimeOffset.UtcNow.AddYears(3));
                RuleFor(x => x.Reason).NotContainScriptTag().MaximumLength(500);
            }
        }
    }
}
