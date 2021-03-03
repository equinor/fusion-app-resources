using FluentValidation;
using System;
using Fusion.AspNetCore.Api;
using Microsoft.Extensions.DependencyInjection;
using Fusion.Resources.Domain;

namespace Fusion.Resources.Api.Controllers
{
    public class PatchInternalRequestRequest : PatchRequest
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
            }
        }

        #endregion
    }
}
