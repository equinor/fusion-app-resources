using FluentValidation;
using System;

namespace Fusion.Resources.Api.Controllers
{
    public class ProposalParametersRequest
    {
        public enum ApiProposalScope { Default, InstanceOnly }


        // Placeholder
        //public enum ApiProposalType {  }

        public DateTime? ChangeDateFrom { get; set; }
        public DateTime? ChangeDateTo { get; set; }

        public string Scope { get; set; } = $"{ApiProposalScope.Default}";

        /// <summary>
        /// Not in use for now, but will most likely be relevant  
        /// Could be used to shape the proposed changes object.
        /// </summary>
        /// 
        /// <remarks>
        /// Setting private set to disable
        /// </remarks>
        public string? Type { get; private set; }


        public Domain.ProposalChangeScope ResolveScope() => Enum.Parse<Domain.ProposalChangeScope>(Scope, true);


        public class Validator : AbstractValidator<ProposalParametersRequest>
        {
            public Validator()
            {
                RuleFor(x => x.Scope)
                    .IsEnumName(typeof(ApiProposalScope), false)
                    .WithMessage((req, p) => $"'{p}' is not valid, allowed values are [{string.Join(", ", Enum.GetNames<ApiProposalScope>())}]");

                RuleFor(x => x.ChangeDateTo)
                    .GreaterThan(r => r.ChangeDateFrom)
                    .WithMessage("To date cannot be before from date")
                    .When(x => x.ChangeDateFrom != null && x.ChangeDateTo != null);

                RuleFor(x => x.ChangeDateTo)
                    .Must(x => x!.Value.TimeOfDay == TimeSpan.Zero)
                    .WithMessage("To date must be a date without time or time set to 00:00:00")
                    .When(x => x.ChangeDateTo != null);

                RuleFor(x => x.ChangeDateFrom)
                    .NotNull()
                    .WithMessage("From date must be defined when to date is specified")
                    .When(x => x.ChangeDateTo != null);

                RuleFor(x => x.ChangeDateFrom)
                    .Must(x => x!.Value.TimeOfDay == TimeSpan.Zero)
                    .WithMessage("From date must be a date without time or time set to 00:00:00")
                    .When(x => x.ChangeDateTo != null);
            }
        }
    }

}
