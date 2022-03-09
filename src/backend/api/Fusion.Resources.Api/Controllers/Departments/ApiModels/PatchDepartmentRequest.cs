using FluentValidation;
using Fusion.AspNetCore.Api;
using System;

namespace Fusion.Resources.Api.Controllers
{
    /// <summary>
    /// Update internal configuration for the department in the context of this service.
    /// </summary>
    public class PatchDepartmentRequest : PatchRequest
    {
        public PatchProperty<DepartmentAutoApprovalRequest> AutoApproval { get; set; } = new();
    

        public class Validator : AbstractValidator<PatchDepartmentRequest>
        {
            public Validator()
            {
                RuleFor(x => x.AutoApproval.Value)
                    .SetValidator(new DepartmentAutoApprovalRequest.Validator())
                    .OverridePropertyName("autoApproval")
                    .When(x => x.AutoApproval.HasValue);
            }
        }
            
        public class DepartmentAutoApprovalRequest
        {
            public bool Enabled { get; set; }
            public string? Mode { get; set; }

            public class Validator : AbstractValidator<DepartmentAutoApprovalRequest>
            {
                public Validator()
                {
                    RuleFor(x => x.Mode)
                        .NotEmpty()
                        .IsEnumName(typeof(ApiDepartmentAutoApprovalMode), false)
                        .WithMessage($"Invalid value, supported types: {string.Join(", ", Enum.GetNames<ApiDepartmentAutoApprovalMode>())}")
                        .OverridePropertyName("mode");
                }
            }
        }
    }
}
