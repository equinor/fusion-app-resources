using FluentValidation;
using Fusion.Resources.Domain;
using System;

namespace Fusion.Resources.Api.Controllers
{
    public class ContractPositionRequest
    {
        public BasePositionReference BasePosition { get; set; } = null!;
        public string? Name { get; set; }
        public DateTime AppliesFrom { get; set; }
        public DateTime AppliesTo { get; set; }
        public PersonReference AssignedPerson { get; set; } = null!;
        public string? Obs { get; set; }

        public double Workload { get; set; }


        #region Validator

        public class Validator : AbstractValidator<ContractPositionRequest>
        {
            public Validator(IProjectOrgResolver projectOrgResolver)
            {
                RuleFor(x => x.Name).NotContainScriptTag().WithName("name");
                RuleFor(x => x.Obs).NotContainScriptTag().When(x => x.Obs != null).WithName("obs");
                RuleFor(x => x.Workload).GreaterThanOrEqualTo(0).WithMessage("Workload cannot be less than 0").WithName("workload");
                RuleFor(x => x.AppliesTo).GreaterThan(x => x.AppliesFrom).WithMessage(x => $"To date cannot be earlier than from date, {x.AppliesFrom.ToString("dd/MM/yyyy")} -> {x.AppliesTo.ToString("dd/MM/yyyy")}");

                RuleFor(x => x.BasePosition).BeValidBasePosition(projectOrgResolver);
                RuleFor(x => x.AssignedPerson).BeValidPerson().When(x => x.AssignedPerson != null);
            }
        }

        #endregion
    }
}
