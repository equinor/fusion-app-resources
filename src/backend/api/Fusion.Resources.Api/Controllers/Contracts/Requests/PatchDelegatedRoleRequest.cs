using Microsoft.AspNetCore.Mvc;
using System;
using Fusion.AspNetCore.FluentAuthorization;
using FluentValidation;
using Fusion.AspNetCore.Api;

namespace Fusion.Resources.Api.Controllers
{
    [ModelBinder(typeof(PatchRequestBinder))]
    public class PatchDelegatedRoleRequest
    {
        public PatchProperty<DateTimeOffset> ValidTo { get; set; } = new PatchProperty<DateTimeOffset>();

        #region Validator

        public class Validator : AbstractValidator<PatchDelegatedRoleRequest>
        {
            public Validator()
            {
                RuleFor(x => x.ValidTo)
                    .Must(v => v.Value.Date > DateTime.UtcNow.Date && v.Value.Date <= DateTime.UtcNow.AddYears(1).Date)
                    .When(x => x.ValidTo.HasValue)
                    .WithMessage($"Valid to must be a future date, maximum 1 year ahead in time ({DateTime.UtcNow.AddYears(1):yyyy-MM-dd}");
            }
        }

        #endregion
    }
}
