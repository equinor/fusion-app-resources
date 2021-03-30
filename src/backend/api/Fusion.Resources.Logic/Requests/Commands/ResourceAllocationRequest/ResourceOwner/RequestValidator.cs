using Fusion.Resources.Database.Entities;
using System;
using Newtonsoft.Json;
using FluentValidation;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {
        public partial class ResourceOwner
        {
            public class RequestValidator : AbstractValidator<DbResourceAllocationRequest>
            {
                private static DateTime Today = DateTime.UtcNow.Date;

                public RequestValidator()
                {
                    RuleFor(x => x.SubType).NotNull();
                    RuleFor(x => x.SubType).IsEnumName(typeof(SubType.Types), false)
                        .WithMessage($"Subtype must be any of [{string.Join(", ", Enum.GetNames<SubType.Types>())}]");
                        

                    RuleFor(x => x).SetValidator(new AdjustmentValidator()).When(x => new SubType(x.SubType).Value == SubType.Types.Adjustment);
                    RuleFor(x => x).SetValidator(new RemoveResourceValidator()).When(x => new SubType(x.SubType).Value == SubType.Types.RemoveResource);
                    RuleFor(x => x).SetValidator(new ChangeResourceValidator()).When(x => new SubType(x.SubType).Value == SubType.Types.ChangeResource);

                    RuleFor(x => x).Must(x => !IsExpiredSplit(x))
                        .WithMessage("Cannot run request on an expired instance");
                        
                }


                #region Sub type validators

                private class AdjustmentValidator : AbstractValidator<DbResourceAllocationRequest>
                {
                    public AdjustmentValidator()
                    {
                        RuleFor(x => x).Custom((r, ctx) =>
                        {
                            var changes = JsonConvert.DeserializeAnonymousType(r.ProposedChanges ?? "{}", new { workload = (double?)null, location = new { } });

                            var missingAdjustmentChanges = changes.workload is null && changes.location is null;

                            // When split is currently active, a change date is required.
                            if (IsCurrentSplit(r) && HasChangeDate(r) == false)
                                ctx.AddFailure("proposalParameters.changeDateFrom", "When the instance to change is currently active, a date the change is going to take effect is required.");

                            if (missingAdjustmentChanges)
                                ctx.AddFailure("changes", "Either proposed changes or proposed person must be set.");

                        });
                    }
                }

                private class RemoveResourceValidator : AbstractValidator<DbResourceAllocationRequest>
                {
                    public RemoveResourceValidator()
                    {
                        RuleFor(x => x.ProposalParameters)
                            .Must(x => x.ChangeFrom != null || x.ChangeTo != null)
                            .WithMessage("When the instance to change is currently active, a date the change is going to take effect is required.")
                            .When(x => IsCurrentSplit(x));

                        RuleFor(x => x.OrgPositionInstance.AssignedToUniqueId)
                            .NotNull()
                            .NotEmpty()
                            .WithMessage("There is no person assigned to the instance. Person might already be removed?");
                    }
                }

                private class ChangeResourceValidator : AbstractValidator<DbResourceAllocationRequest>
                {
                    public ChangeResourceValidator()
                    {
                        RuleFor(x => x.ProposalParameters)
                            .Must(x => x.ChangeFrom != null || x.ChangeTo != null)
                            .WithMessage("When the instance to change is currently active, a date the change is going to take effect is required.")
                            .When(x => IsCurrentSplit(x));

                        RuleFor(x => x.ProposedPerson.AzureUniqueId)
                            .NotNull()
                            .NotEmpty()
                            .WithMessage("Must specify person to change to");
                    }
                }

                #endregion

                private static bool HasChangeDate(DbResourceAllocationRequest request)
                    => request.ProposalParameters.ChangeTo is not null || request.ProposalParameters.ChangeFrom is not null;

                private static bool IsCurrentSplit(DbResourceAllocationRequest request) 
                    => request.OrgPositionInstance.AppliesFrom.Date <= Today && request.OrgPositionInstance.AppliesTo >= Today;

                private static bool IsExpiredSplit(DbResourceAllocationRequest request) 
                    => request.OrgPositionInstance.AppliesTo <= Today;
            }
        }
    }
}
