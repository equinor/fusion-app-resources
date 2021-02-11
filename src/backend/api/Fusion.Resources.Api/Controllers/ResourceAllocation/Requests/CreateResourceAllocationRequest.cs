using FluentValidation;
using System;
using System.Linq;
using System.Text.Json.Serialization;
using FluentValidation.Results;
using FluentValidation.Validators;

namespace Fusion.Resources.Api.Controllers
{
    public class CreateResourceAllocationRequest
    {
        internal Guid? Id { get; set; }
        internal Guid? ProjectId { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ApiAllocationRequestType Type { get; set; }
        public string? Discipline { get; set; }
        public Guid? OrgPositionId { get; set; }
        public ApiPositionInstance? OrgPositionInstance { get; set; }
        public string? AdditionalNote { get; set; }
        public ApiPropertiesCollection? ProposedChanges { get; set; }
        public Guid? ProposedPersonAzureUniqueId { get; set; }
        public bool? IsDraft { get; set; }


        #region Validator

        public class Validator : AbstractValidator<CreateResourceAllocationRequest>
        {
            public Validator()
            {
                RuleFor(x => x.ProjectId).NotEmpty().When(x => x.ProjectId != null);

                RuleFor(x => x.Discipline).NotContainScriptTag().MaximumLength(500);
                RuleFor(x => x.AdditionalNote).NotContainScriptTag().MaximumLength(5000);

                RuleFor(x => x.OrgPositionId).NotEmpty().When(x => x.OrgPositionId != null);
                RuleFor(x => x.OrgPositionInstance).SetValidator(PositionInstanceValidator).When(x => x.OrgPositionInstance != null);
                RuleFor(x => x.ProposedChanges).SetValidator(ProposedChangesValidator).When(x => x.ProposedChanges != null);

                RuleFor(x => x.ProposedPersonAzureUniqueId).NotEmpty().When(x => x.ProposedPersonAzureUniqueId != null);
            }


            private static IPropertyValidator ProposedChangesValidator => new CustomValidator<ApiPropertiesCollection>(
                (prop, context) =>
                {
                    foreach (var k in prop.Keys.Where(k => k.Length > 100))
                    {
                        context.AddFailure(new ValidationFailure($"{context.JsPropertyName()}.key",
                            "Key cannot exceed 100 characters", k));
                    }

                });

            private static IPropertyValidator PositionInstanceValidator => new CustomValidator<ApiPositionInstance>(
                (position, context) =>
                {
                    if (position == null) return;

                    if (position.AppliesTo < position.AppliesFrom)
                        context.AddFailure(new ValidationFailure($"{context.JsPropertyName()}.appliesTo",
                            $"To date cannot be earlier than from date, {position.AppliesFrom:dd/MM/yyyy} -> {position.AppliesTo:dd/MM/yyyy}",
                            $"{position.AppliesFrom:dd/MM/yyyy} -> {position.AppliesTo:dd/MM/yyyy}"));


                    if (position.Obs?.Length > 30)
                        context.AddFailure(new ValidationFailure($"{context.JsPropertyName()}.obs",
                            "Obs cannot exceed 30 characters", position.Obs));

                    if (position.Workload < 0)
                        context.AddFailure(new ValidationFailure($"{context.JsPropertyName()}.workload",
                            "Workload cannot be less than 0", position.Workload));

                    if (position.Workload > 100)
                        context.AddFailure(new ValidationFailure($"{context.JsPropertyName()}.workload",
                            "Workload cannot be more than 1000", position.Workload));
                });
        }

        #endregion
    }
}
