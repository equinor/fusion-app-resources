using FluentValidation;
using System;
using System.Linq;
using System.Text.Json.Serialization;
using FluentValidation.Results;
using FluentValidation.Validators;

namespace Fusion.Resources.Api.Controllers
{
    public class CreateProjectAllocationRequest
    {
        internal Guid? Id { get; set; }
        public string? Discipline { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ApiResourceAllocationRequest.ApiAllocationRequestType Type { get; set; }
        public Guid OrgPositionId { get; set; }
        public ApiPositionInstance OrgPositionInstance { get; set; } = new ApiPositionInstance();
        public string? AdditionalNote { get; set; }
        public ApiPropertiesCollection ProposedChanges { get; set; } = new ApiPropertiesCollection();
        public Guid ProposedPersonId { get; set; }
        public bool? IsDraft { get; set; }


        #region Validator

        public class Validator : AbstractValidator<CreateProjectAllocationRequest>
        {
            public Validator()
            {
                RuleFor(x => x.Discipline).NotContainScriptTag().MaximumLength(500);
                RuleFor(x => x.AdditionalNote).NotContainScriptTag().MaximumLength(5000);

                RuleFor(x => x.OrgPositionId).NotEmpty();
                RuleFor(x => x.OrgPositionInstance).SetValidator(PositionInstanceValidator);
                RuleFor(x => x.ProposedChanges).SetValidator(ProposedChangesValidator);

                RuleFor(x => x.ProposedPersonId).NotEmpty();
            }


            private static IPropertyValidator ProposedChangesValidator => new CustomValidator<ApiPropertiesCollection>((prop, context) =>
            {
                foreach (var k in prop.Keys.Where(k => k.Length > 100))
                {
                    context.AddFailure(new ValidationFailure($"{context.JsPropertyName()}.key", "Key cannot exceed 100 characters", k));
                }

            });

            private static IPropertyValidator PositionInstanceValidator => new CustomValidator<ApiPositionInstance>((position, context) =>
            {

                if (position.AppliesTo < position.AppliesFrom)
                    context.AddFailure(new ValidationFailure($"{context.JsPropertyName()}.appliesTo",
                        $"To date cannot be earlier than from date, {position.AppliesFrom:dd/MM/yyyy} -> {position.AppliesTo:dd/MM/yyyy}",
                        $"{position.AppliesFrom:dd/MM/yyyy} -> {position.AppliesTo:dd/MM/yyyy}"));


                if (position.Obs.Length > 30)
                    context.AddFailure(new ValidationFailure($"{context.JsPropertyName()}.obs", "Obs cannot exceed 30 characters", position.Obs));

                if (position.Workload < 0)
                    context.AddFailure(new ValidationFailure($"{context.JsPropertyName()}.workload", "Workload cannot be less than 0", position.Workload));

                if (position.Workload > 100)
                    context.AddFailure(new ValidationFailure($"{context.JsPropertyName()}.workload", "Workload cannot be more than 1000", position.Workload));

            });
        }

        #endregion
    }
}
