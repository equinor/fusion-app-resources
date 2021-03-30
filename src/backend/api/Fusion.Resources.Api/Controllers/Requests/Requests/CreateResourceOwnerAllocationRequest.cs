using FluentValidation;
using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Fusion.Integration.Org;
using Microsoft.Extensions.Logging;

namespace Fusion.Resources.Api.Controllers
{
    public class CreateResourceOwnerAllocationRequest
    {
        public string Type { get; set; } = null!;

        public string SubType { get; set; } = null!;

        public Guid OrgPositionId { get; set; }
        public Guid OrgPositionInstanceId { get; set; }

        public string? AdditionalNote { get; set; }
        public ApiPropertiesCollection? ProposedChanges { get; set; }
        public ProposalParametersRequest? ProposalParameters { get; set; }

        public Guid? ProposedPersonAzureUniqueId { get; set; }


        #region Validator

        public class Validator : AbstractValidator<CreateResourceOwnerAllocationRequest>
        {
            /// <summary>
            /// Allowed types for this request type
            /// </summary>
            private enum ApiResourceOwnerRequestType { ResourceOwnerChange }
            private enum ApiResourceOwnerRequestSubType { Adjustment, ChangeResource, RemoveResource }

            public Validator()
            {
                RuleFor(x => x.Type).NotNull().NotEmpty();                
                RuleFor(x => x.Type).IsEnumName(typeof(ApiResourceOwnerRequestType), false)
                    .WithMessage((req, p) => $"Type '{p}' is not valid, allowed values are [{string.Join(", ", Enum.GetNames<ApiResourceOwnerRequestType>())}]");

                RuleFor(x => x.SubType).IsEnumName(typeof(ApiResourceOwnerRequestSubType), false)
                    .WithMessage((req, p) => $"Type '{p}' is not valid, allowed values are [{string.Join(", ", Enum.GetNames<ApiResourceOwnerRequestSubType>())}]");

                RuleFor(x => x.AdditionalNote).NotContainScriptTag().MaximumLength(5000);

                RuleFor(x => x.OrgPositionId).NotEmpty();
                RuleFor(x => x.OrgPositionInstanceId).NotEmpty();


                RuleFor(x => x.ProposedChanges).BeValidProposedChanges().When(x => x.ProposedChanges != null);

                RuleFor(x => x.ProposedPersonAzureUniqueId).NotEmpty().When(x => x.ProposedPersonAzureUniqueId != null);

                RuleFor(x => x.ProposalParameters!).SetValidator(new ProposalParametersRequest.Validator())
                    .When(x => x.ProposalParameters != null);

                RuleFor(x => x)
                    .CustomAsync(async (req, context, ct) =>
                    {
                        var orgResolver = context.GetServiceProvider().GetRequiredService<IProjectOrgResolver>();
                        var logger = context.GetServiceProvider().GetRequiredService<ILogger<Validator>>();

                        try
                        {
                            var position = await orgResolver.ResolvePositionAsync(req.OrgPositionId);

                            if (position is null)
                                context.AddFailure("Position does not exist");
                            else
                            {
                                var instance = position?.Instances.FirstOrDefault(i => i.Id == req.OrgPositionInstanceId);
                                if (instance is null)
                                    context.AddFailure($"Instance with id '{req.OrgPositionInstanceId}' does not exist on position");
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Could not resolve position from org chart");
                            context.AddFailure($"Could not resolve position from org chart: {ex.Message}");
                        }
                    });
            }
        }

        #endregion

        public Domain.InternalRequestType ResolveType() => Enum.Parse<Domain.InternalRequestType>(Type, true);
    }

}
