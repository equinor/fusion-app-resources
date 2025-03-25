using FluentValidation;
using System;
using Fusion.AspNetCore.Api;
using Microsoft.Extensions.DependencyInjection;
using Fusion.Resources.Domain;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Collections.Generic;
using Fusion.Resources.Database;
using Microsoft.EntityFrameworkCore;
using MediatR;
using System.Diagnostics;
using Fusion.Resources.Domain.Commands;

namespace Fusion.Resources.Api.Controllers
{
    public class PatchInternalRequestRequest : PatchRequest
    {
        public PatchProperty<string?> AdditionalNote { get; set; } = new();
        public PatchProperty<string?> AssignedDepartment { get; set; } = new();
        public PatchProperty<Guid?> ProposedPersonAzureUniqueId { get; set; } = new();
        public PatchProperty<ApiPropertiesCollection?> ProposedChanges { get; set; } = new();
        public PatchProperty<ApiPropertiesCollection?> Properties { get; set; } = new();

        public PatchProperty<ProposalParametersRequest> ProposalParameters { get; set; } = new();
        public PatchProperty<List<PersonReference>?> Candidates { get; set; } = new();
        #region Validator


        public class Validator : AbstractValidator<PatchInternalRequestRequest>
        {
            public Validator(IServiceProvider services, IMediator mediator)
            {
                RuleFor(x => x.ProposedPersonAzureUniqueId)
                    .MustAsync(async (req, p, context, cancel) =>
                    {
                        
                        if (p.Value.HasValue)
                        {
                            var profileResolver = services.GetRequiredService<IProfileService>();
                            var person = await profileResolver.ResolveProfileAsync(p.Value.Value);
                            return person is not null;
                        }

                        return false;
                    })
                    .When(x => x.ProposedPersonAzureUniqueId.HasValue && x.ProposedPersonAzureUniqueId.Value.HasValue);


                RuleFor(x => x.AssignedDepartment.Value)
                    .BeValidOrgUnit(services)
                    .WithMessage("Invalid department specified")
                    .WithName("assignedDepartment")
                    .When(x => x.AssignedDepartment.HasValue && x.AssignedDepartment.Value != null);

                RuleFor(x => x.ProposedChanges)
                    .Custom((x, context) =>
                    {
                        if (x.HasValue && x.Value != null)
                        {
                            string[] allowedProperties = ["appliesFrom", "appliesTo", "location", "workload", "basePosition"];

                            var isInvalid = false;
                            foreach (var key in x.Value.Keys)
                            {
                                if (!allowedProperties.Any(p => string.Equals(p, key, StringComparison.OrdinalIgnoreCase)))
                                {
                                    context.AddFailure($"Key '{key}' is not valid");
                                    isInvalid = true;
                                }
                            }

                            if (isInvalid)
                            {
                                context.AddFailure($"Allowed keys are {string.Join(", ", allowedProperties.Select(p => p.ToLowerFirstChar()))}");
                            }


                        }
                    });

                RuleFor(x => x.ProposalParameters.Value).SetValidator(new ProposalParametersRequest.Validator())
                    .OverridePropertyName(x => x.ProposalParameters)
                    .When(x => x.ProposalParameters != null && x.ProposalParameters.HasValue);
            }
        }

        #endregion
    }
}
