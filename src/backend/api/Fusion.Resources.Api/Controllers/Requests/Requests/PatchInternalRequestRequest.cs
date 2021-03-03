using FluentValidation;
using System;
using Fusion.AspNetCore.Api;
using Microsoft.Extensions.DependencyInjection;
using Fusion.Resources.Domain;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Fusion.Resources.Api.Controllers
{
    [ModelBinder(typeof(PatchRequestBinder))]
    public class PatchInternalRequestRequest //: PatchRequest
    {
        public PatchProperty<bool> IsDraft { get; set; } = new();
        public PatchProperty<string?> AdditionalNote { get; set; } = new();
        public PatchProperty<string?> AssignedDepartment { get; set; } = new();
        public PatchProperty<Guid?> ProposedPersonAzureUniqueId { get; set; } = new();
        public PatchProperty<ApiPropertiesCollection?> ProposedChanges { get; set; } = new();


        #region Validator


        public class Validator : AbstractValidator<PatchInternalRequestRequest>
        {
            public Validator()
            {
                RuleFor(x => x.ProposedPersonAzureUniqueId)
                    .MustAsync(async (req, p, context, cancel) =>
                    {

                        if (p.Value.HasValue)
                        {
                            var profileResolver = context.GetServiceProvider().GetRequiredService<IProfileService>();
                            var person = await profileResolver.ResolveProfileAsync(p.Value.Value);
                            return person is not null;
                        }

                        return false;
                    })
                    .When(x => x.ProposedPersonAzureUniqueId.HasValue);


                RuleFor(x => x.AssignedDepartment)
                    .Must(d =>
                    {
                        var sectors = PersonController.FetchSectors();
                        return sectors.ContainsKey(d.Value.ToUpper());
                    })
                    .WithMessage("Invalid department specified")
                    .When(x => x.AssignedDepartment.HasValue && x.AssignedDepartment.Value != null);

                RuleFor(x => x.ProposedChanges)
                    .Custom((x, context) =>
                    {
                        if (x.HasValue && x.Value != null)
                        {
                            var t = typeof(ApiClients.Org.ApiPositionInstanceV2);
                            var allowedProperties = t.GetProperties();

                            var isInvalid = false;
                            foreach (var key in x.Value.Keys)
                            {
                                
                                if (!allowedProperties.Any(p => string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase)))
                                {
                                    context.AddFailure($"Key '{key}' is not valid");
                                    isInvalid = true;
                                }
                            }
                            
                            if (isInvalid)
                            {
                                context.AddFailure($"Allowed keys are {string.Join(", ", allowedProperties.Select(p => p.Name.ToLowerFirstChar()))}");
                            }


                        }
                    });
            }
        }

        #endregion
    }
}
