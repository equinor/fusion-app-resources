using System;
using FluentValidation;

namespace Fusion.Resources.Api.Controllers
{
    public class ReplaceContractPersonnelRequest
    {
        public string UPN { get; set; } = null!;
        public Guid AzureUniquePersonId { get; set; }

        #region Validation

        public class Validator : AbstractValidator<ReplaceContractPersonnelRequest>
        {
            public Validator()
            {
                RuleFor(x => x.UPN).NotEmpty();
                RuleFor(x => x.UPN).NotContainScriptTag();
                RuleFor(x => x.UPN).MaximumLength(200);

                RuleFor(x => x.AzureUniquePersonId).NotEmpty();
            }

        }

        #endregion
    }
}
