using System;
using System.Text.Json.Serialization;
using FluentValidation;
using Fusion.Resources.Domain.Commands;
using Fusion.Resources.Domain;

namespace Fusion.Resources.Api.Controllers
{
    public class CreatePersonAbsenceRequest
    {
        public string? Comment { get; set; }
        public DateTimeOffset AppliesFrom { get; set; }
        public DateTimeOffset? AppliesTo { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ApiPersonAbsence.ApiAbsenceType Type { get; set; }

        public string? Grade { get; set; }


        public void LoadCommand(CreatePersonAbsence command)
        {
            command.Comment = Comment;
            command.AppliesFrom = AppliesFrom;
            command.AppliesTo = AppliesTo;
            command.Type = Enum.Parse<QueryAbsenceType>($"{Type}", true);
            command.Grade = Grade;
        }


        #region Validation

        public class Validator : AbstractValidator<CreatePersonAbsenceRequest>
        {
            public Validator()
            {
                RuleFor(x => x.Comment).NotContainScriptTag();
                RuleFor(x => x.Comment).MaximumLength(5000);

                RuleFor(x => x.Grade).NotContainScriptTag();
                RuleFor(x => x.Grade).MaximumLength(4);

                RuleFor(x => x.AppliesTo).GreaterThan(x => x.AppliesFrom)
                    .WithMessage(x => "To date cannot be earlier than from date");

            }
        }

        #endregion
    }
}
