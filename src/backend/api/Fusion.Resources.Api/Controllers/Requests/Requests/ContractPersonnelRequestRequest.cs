using FluentValidation;
using FluentValidation.Results;
using FluentValidation.Validators;
using Fusion.Integration.Org;
using Fusion.Resources.Database;
using Fusion.Resources.Domain;
using System;

namespace Fusion.Resources.Api.Controllers
{
    public class ContractPersonnelRequestRequest
    {
        public Guid? Id { get; set; }
        public string? Description { get; set; }

        public RequestPosition Position { get; set; } = null!;
        public PersonReference Person { get; set; } = null!;

        public Guid? OriginalPositionId { get; set; }
    

        public class RequestPosition
        {
            /// <summary>
            /// Existing org chart position id.
            /// </summary>
            public Guid? Id { get; set; }

            public BasePositionReference BasePosition { get; set; } = null!;
            public string Name { get; set; } = null!;
            public DateTime AppliesFrom { get; set; }
            public DateTime AppliesTo { get; set; }
            public string? Obs { get; set; } 

            public TaskOwnerReference TaskOwner { get; set; } = null!;
            public double Workload { get; set; }
        }


        #region Validator

        public class Validator : AbstractValidator<ContractPersonnelRequestRequest>
        {
            private readonly IProjectOrgResolver projectOrgResolver;

            public Validator(IProjectOrgResolver projectOrgResolver)
            {
                RuleFor(x => x.Description).NotContainScriptTag().When(x => x.Description != null).WithName("description");
                RuleFor(x => x.Position).NotNull().WithName("position").WithMessage("Position details must be specified");
                RuleFor(x => x.Person).BeValidPerson().When(x => x.Person != null);
                RuleFor(x => x.Position).SetValidator(RequestPositionValidator).When(x => x.Position != null);

                RuleFor(x => x.Position.Name).NotContainScriptTag().When(x => x.Position != null).WithName("position.name");
                RuleFor(x => x.Position.Obs).NotContainScriptTag().When(x => x.Position != null).WithName("position.obs");


                RuleFor(x => x.Position.BasePosition).BeValidBasePosition(projectOrgResolver)
                    .When(x => x.Position != null);

                RuleFor(x => x.Position.TaskOwner.PositionId).BeExistingContractPositionId(projectOrgResolver)
                    .When(x => x.Position != null && x.Position.TaskOwner != null);

                RuleFor(x => x.OriginalPositionId).BeValidChangeRequestPosition(projectOrgResolver)
                    .When(x => x.OriginalPositionId.HasValue);

                this.projectOrgResolver = projectOrgResolver;
            }

            private IPropertyValidator RequestPositionValidator => new CustomValidator<RequestPosition>((position, context) =>
            {

                if (position.AppliesTo < position.AppliesFrom)
                    context.AddFailure(new ValidationFailure($"{context.JsPropertyName()}.appliesTo", 
                        $"To date cannot be earlier than from date, {position.AppliesFrom.ToString("dd/MM/yyyy")} -> {position.AppliesTo.ToString("dd/MM/yyyy")}", 
                        $"{position.AppliesFrom.ToString("dd/MM/yyyy")} -> {position.AppliesTo.ToString("dd/MM/yyyy")}"));

                if (position.Name != null && position.Name.Length > 150)
                    context.AddFailure(new ValidationFailure($"{context.JsPropertyName()}.name", "Name cannot exceed 150 characters", position.Obs));

                if (position.Obs != null && position.Obs.Length > 30)
                    context.AddFailure(new ValidationFailure($"{context.JsPropertyName()}.obs", "Obs cannot exceed 30 characters", position.Obs));

                if (position.Workload < 0)
                    context.AddFailure(new ValidationFailure($"{context.JsPropertyName()}.workload", "Workload cannot be less than 0", position.Workload));

                if (position.BasePosition == null)
                    context.AddFailure(new ValidationFailure($"{context.JsPropertyName()}.basePosition", "Base position must be specified and refer to valid org chart base position id."));

                if (position.BasePosition != null)
                {
                    if (position.BasePosition.Id == Guid.Empty)
                        context.AddFailure(new ValidationFailure($"{context.JsPropertyName()}.basePosition.id", "Base position id cannot be empty."));
                }

                if (position.TaskOwner != null && position.TaskOwner.PositionId.HasValue)
                {
                    if (position.TaskOwner.PositionId.Value == Guid.Empty)
                        context.AddFailure(new ValidationFailure($"{context.JsPropertyName()}.taskOwner.positionId", "Task owner Position id cannot be empty when provided."));
                }

            });
        }

        #endregion 

    }

}
