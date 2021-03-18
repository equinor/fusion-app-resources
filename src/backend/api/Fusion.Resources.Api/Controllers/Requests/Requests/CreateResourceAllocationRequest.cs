using FluentValidation;
using System;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Fusion.Integration.Org;
using Microsoft.Extensions.Logging;

namespace Fusion.Resources.Api.Controllers
{
    public class CreateResourceAllocationRequest
    {
        public string Type { get; set; } = null!;
        
        // Will be auto-detected by the api, but can be specified.
        public string? SubType { get; set; }

        public string? AssignedDepartment { get; set; }
        
        // All requests should be created as drafts. Initializing the workflow shoud set the flag to false.
        //public bool? IsDraft { get; set; }

        // Not required unless created from the resource owner side. Change requests.
        internal Guid? OrgProjectId { get; set; }
        public Guid OrgPositionId { get; set; }
        public Guid OrgPositionInstanceId { get; set; }
        
        public string? AdditionalNote { get; set; }
        public ApiPropertiesCollection? ProposedChanges { get; set; }


        public Guid? ProposedPersonAzureUniqueId { get; set; }


        #region Validator

        public class Validator : AbstractValidator<CreateResourceAllocationRequest>
        {
            public Validator()
            {
                RuleFor(x => x.Type).NotNull().NotEmpty();
                RuleFor(x => x.Type).IsEnumName(typeof(ApiAllocationRequestType), false)
                    .WithMessage((req, p) => $"Type '{p}' is not valid, allowed values are [{string.Join(", ", Enum.GetNames<ApiAllocationRequestType>())}]");

                RuleFor(x => x.OrgProjectId).NotEmpty().When(x => x.OrgProjectId != null);

                RuleFor(x => x.AssignedDepartment).NotContainScriptTag().MaximumLength(500);
                RuleFor(x => x.AdditionalNote).NotContainScriptTag().MaximumLength(5000);

                RuleFor(x => x.OrgPositionId).NotEmpty();
                RuleFor(x => x.OrgPositionInstanceId).NotEmpty();


                RuleFor(x => x.ProposedChanges).BeValidProposedChanges().When(x => x.ProposedChanges != null);

                RuleFor(x => x.ProposedPersonAzureUniqueId).NotEmpty().When(x => x.ProposedPersonAzureUniqueId != null);


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

        public Domain.InternalRequestType ResolveType() => Type == "normal" ? Domain.InternalRequestType.Allocation : Enum.Parse<Domain.InternalRequestType>(Type, true);
    }

    public class CreateResourceOwnerAllocationRequest
    {
        public string Type { get; set; } = null!;

        public string? SubType { get; set; }

        public Guid OrgPositionId { get; set; }
        public Guid OrgPositionInstanceId { get; set; }

        public string? AdditionalNote { get; set; }
        public ApiPropertiesCollection? ProposedChanges { get; set; }


        public Guid? ProposedPersonAzureUniqueId { get; set; }


        #region Validator

        public class Validator : AbstractValidator<CreateResourceOwnerAllocationRequest>
        {
            /// <summary>
            /// Allowed types for this request type
            /// </summary>
            private enum ApiResourceOwnerRequestType { ResourceOwnerChange }

            public Validator()
            {
                RuleFor(x => x.Type).NotNull().NotEmpty();                
                RuleFor(x => x.Type).IsEnumName(typeof(ApiResourceOwnerRequestType), false)
                    .WithMessage((req, p) => $"Type '{p}' is not valid, allowed values are [{string.Join(", ", Enum.GetNames<ApiResourceOwnerRequestType>())}]");

                RuleFor(x => x.AdditionalNote).NotContainScriptTag().MaximumLength(5000);

                RuleFor(x => x.OrgPositionId).NotEmpty();
                RuleFor(x => x.OrgPositionInstanceId).NotEmpty();


                RuleFor(x => x.ProposedChanges).BeValidProposedChanges().When(x => x.ProposedChanges != null);

                RuleFor(x => x.ProposedPersonAzureUniqueId).NotEmpty().When(x => x.ProposedPersonAzureUniqueId != null);


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
