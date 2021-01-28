using System;
using FluentValidation;
using Fusion.Resources.Domain.Commands;
using Fusion.Resources.Domain;

namespace Fusion.Resources.Api.Controllers
{
    public class UpdateEmploymentStatusRequest
    {
        public string? Comment { get; set; }
        public DateTimeOffset AppliesFrom { get; set; }
        public DateTimeOffset? AppliesTo { get; set; }
        public QueryAbsenceType Type { get; set; }



        public void LoadCommand(UpdatePersonAbsence command)
        {
            command.Comment = Comment;
            command.AppliesFrom = AppliesFrom;
            command.AppliesTo = AppliesTo;
            command.Type = Type;

        }

        #region Validation

        public class Validator : AbstractValidator<UpdateEmploymentStatusRequest>
        {
            public Validator()
            {
                RuleFor(x => x.Comment).NotContainScriptTag();
                RuleFor(x => x.Comment).MaximumLength(5000);

                RuleFor(x => x.AppliesTo).GreaterThan(x => x.AppliesFrom)
                    .WithMessage(x => "To date cannot be earlier than from date");

            }

        }

        #endregion
    }
}
