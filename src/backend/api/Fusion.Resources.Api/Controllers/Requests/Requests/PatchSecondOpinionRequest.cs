using FluentValidation;
using Fusion.AspNetCore.Api;
using Fusion.Resources.Domain;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Resources.Api.Controllers
{
    public class PatchSecondOpinionRequest : PatchRequest
    {
        public PatchProperty<string> Title { get; set; } = new();
        public PatchProperty<string> Description { get; set; } = new();
        public PatchProperty<List<PersonReference>> AssignedTo { get; set; } = new();

        public class Validator : AbstractValidator<PatchSecondOpinionRequest>
        {
            public Validator(IServiceProvider services)
            {
                RuleFor(x => x.Title.Value).NotEmpty().When(x => x.Title.HasValue);
                RuleFor(x => x.Description.Value).NotEmpty().When(x => x.Description.HasValue);
                RuleFor(x => x.AssignedTo)
                    .MustAsync(async (req, assignedToIds, context, cancel) =>
                    {
                        var profileResolver = services.GetRequiredService<IProfileService>();
                        var resolved = await profileResolver.ResolveProfilesAsync(assignedToIds.Value.Select(x => (PersonId)x));

                        if (resolved is null) return false;

                        var failed = resolved
                            .Where(x => !x.Success)
                            .Select(x => x.Identifier);

                        if (failed.Any())
                        {
                            context.MessageFormatter.AppendArgument("FailedIds", string.Join(',', failed));
                            return false;
                        }

                        return true;
                    })
                    .WithMessage("Failed to resolve the following ids: {FailedIds}")
                    .When(x => x.AssignedTo.HasValue);
            }
        }
    }
}
