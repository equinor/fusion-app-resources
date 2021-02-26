using FluentValidation;
using System;

namespace Fusion.Resources.Api.Controllers
{
    public class UpdateResourceAllocationRequest
    {
        internal Guid? Id { get; set; }
        internal Guid? ProjectId { get; set; }
        public string? AssignedDepartment { get; set; }
        public string? Discipline { get; set; }
        public string? AdditionalNote { get; set; }
        public ApiPropertiesCollection? ProposedChanges { get; set; }
        public Guid? ProposedPersonAzureUniqueId { get; set; }
        public bool? IsDraft { get; set; }


        #region Validator

        public class Validator : AbstractValidator<UpdateResourceAllocationRequest>
        {
            public Validator()
            {
                RuleFor(x => x.ProjectId).NotEmpty().When(x => x.ProjectId != null);

                RuleFor(x => x.AssignedDepartment).NotContainScriptTag().MaximumLength(500);
                RuleFor(x => x.Discipline).NotContainScriptTag().MaximumLength(500);
                RuleFor(x => x.AdditionalNote).NotContainScriptTag().MaximumLength(5000);

                RuleFor(x => x.ProposedChanges).BeValidProposedChanges().When(x => x.ProposedChanges != null);

                RuleFor(x => x.ProposedPersonAzureUniqueId).NotEmpty().When(x => x.ProposedPersonAzureUniqueId != null);
            }
        }

        #endregion
    }
}
