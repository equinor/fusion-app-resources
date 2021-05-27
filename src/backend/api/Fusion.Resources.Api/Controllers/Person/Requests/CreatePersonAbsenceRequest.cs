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
        public DateTime AppliesFrom { get; set; }
        public DateTime? AppliesTo { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ApiPersonAbsence.ApiAbsenceType Type { get; set; }

        public double? AbsencePercentage { get; set; }

        public bool IsPrivate { get; set; }
        public ApiTaskDetails? TaskDetails { get; set; }

        public void LoadCommand(CreatePersonAbsence command)
        {
            DateTime.SpecifyKind(AppliesFrom, DateTimeKind.Utc);
            if(AppliesTo.HasValue) DateTime.SpecifyKind(AppliesTo.Value, DateTimeKind.Utc);

            command.Comment = Comment;
            command.AppliesFrom = AppliesFrom;
            command.AppliesTo = AppliesTo;
            command.Type = Enum.Parse<QueryAbsenceType>($"{Type}", true);
            command.AbsencePercentage = AbsencePercentage;
            command.IsPrivate = IsPrivate;
            command.BasePositionId = TaskDetails?.BasePositionId;
            command.TaskName = TaskDetails?.TaskName;
            command.RoleName = TaskDetails?.RoleName;
            command.Location = TaskDetails?.Location;
        }


        #region Validation

        public class Validator : AbstractValidator<CreatePersonAbsenceRequest>
        {
            public Validator()
            {
                RuleFor(x => x.Comment).NotContainScriptTag();
                RuleFor(x => x.Comment).MaximumLength(5000);

                RuleFor(x => x.AbsencePercentage).LessThanOrEqualTo(100).When(x => x.AbsencePercentage != null);

                RuleFor(x => x.AppliesTo).GreaterThan(x => x.AppliesFrom)
                    .WithMessage(x => "To date cannot be earlier than from date");

                RuleFor(x => x.TaskDetails)
                    .Empty()
                    .When(x => x.Type != ApiPersonAbsence.ApiAbsenceType.OtherTasks)
                    .WithMessage("Cannot set task details when type is not 'other tasks'.");
            }
        }

        #endregion
    }
}
