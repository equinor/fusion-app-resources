﻿using FluentValidation;
using Fusion.Integration;
using Fusion.Integration.Org;
using Fusion.Resources.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Fusion.Resources.Api.Controllers
{
    public class BatchCreateResourceAllocationRequest
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
        public List<Guid> OrgPositionInstanceIds { get; set; } = new();

        public string? AdditionalNote { get; set; }
        [JsonConverter(typeof(Json.DictionaryStringObjectJsonConverter))]
        public Dictionary<string, object>? ProposedChanges { get; set; }


        public Guid? ProposedPersonAzureUniqueId { get; set; }


        #region Validator

        public class Validator : AbstractValidator<BatchCreateResourceAllocationRequest>
        {
            public Validator(IMediator mediator, IFusionProfileResolver profileResolver, IServiceProvider services)
            {
                RuleFor(x => x.Type).NotNull().NotEmpty();
                RuleFor(x => x.Type).IsEnumName(typeof(ApiAllocationRequestType), false)
                    .WithMessage((req, p) => $"Type '{p}' is not valid, allowed values are [{string.Join(", ", Enum.GetNames<ApiAllocationRequestType>())}]");

                RuleFor(x => x.OrgProjectId).NotEmpty().When(x => x.OrgProjectId != null);

                RuleFor(x => x.AssignedDepartment).NotContainScriptTag().MaximumLength(100);
                RuleFor(x => x.AdditionalNote).NotContainScriptTag().MaximumLength(5000);

                RuleFor(x => x.OrgPositionId).NotEmpty();
                RuleFor(x => x.OrgPositionInstanceIds).NotEmpty();


                RuleFor(x => x.ProposedChanges).BeValidProposedChanges().When(x => x.ProposedChanges != null);

                RuleFor(x => x.ProposedPersonAzureUniqueId).NotEmpty().When(x => x.ProposedPersonAzureUniqueId != null);


                RuleFor(x => x)
                    .CustomAsync(async (req, context, ct) =>
                    {
                        var orgResolver = services.GetRequiredService<IProjectOrgResolver>();
                        var logger = services.GetRequiredService<ILogger<Validator>>();

                        try
                        {
                            var position = await orgResolver.ResolvePositionAsync(req.OrgPositionId);

                            if (position is null)
                                context.AddFailure("Position does not exist");
                            else
                            {
                                foreach (var instanceId in req.OrgPositionInstanceIds)
                                {
                                    var instance = position?.Instances.FirstOrDefault(i => i.Id == instanceId);
                                    if (instance is null)
                                        context.AddFailure($"Instance with id '{instanceId}' does not exist on position");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Could not resolve position from org chart");
                            context.AddFailure($"Could not resolve position from org chart: {ex.Message}");
                        }
                    });

                RuleFor(x => x.ProposedPersonAzureUniqueId)
                    .CustomAsync(async (id, context, ct) =>
                    {
                        if (!id.HasValue) return;

                        var logger = services.GetRequiredService<ILogger<Validator>>();

                        try
                        {
                            var profile = await profileResolver.ResolvePersonBasicProfileAsync(id.Value);
                            if (profile is null) context.AddFailure($"Could not find person with id '{id.Value}'");
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, $"Could not resolve person with id '{id.Value}'.");
                            context.AddFailure($"Could not resolve person with id '{id.Value}': {ex.Message}");
                        }

                    });
                RuleFor(x => x.AssignedDepartment)
                   .MustAsync(async (d, cancellationToken) =>
                   {
                       if (d is null)
                           return true;

                       var department = await mediator.Send(new GetDepartment(d), cancellationToken);
                       return department is not null;
                   })
                   .WithMessage("Invalid department specified")
                   .When(x => !string.IsNullOrEmpty(x.AssignedDepartment));
            }
        }

        #endregion

        public Domain.InternalRequestType ResolveType() => Type == "normal" ? Domain.InternalRequestType.Allocation : Enum.Parse<Domain.InternalRequestType>(Type, true);
    }

}
