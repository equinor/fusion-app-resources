using FluentValidation;
using Fusion.Resources.Domain;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Resources.Api.Controllers
{
    public class AddSecondOpinionRequest
    {
        public string Description { get; set; } = null!;
        public List<PersonReference> AssignedTo { get; set; } = null!;

        public class Validator : AbstractValidator<AddSecondOpinionRequest>
        {
            public Validator()
            {
                RuleFor(x => x.Description).NotEmpty();
                RuleFor(x => x.AssignedTo).NotEmpty();

                RuleFor(x => x.AssignedTo)
                    .MustAsync(async (req, assignedToIds, context, cancel) =>
                    {
                        var profileResolver = context.GetServiceProvider().GetRequiredService<IProfileService>();
                        var resolved = await profileResolver.ResolveProfilesAsync(assignedToIds.Select(x => (PersonId)x));

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
                    .WithMessage("Failed to resolve the following ids: {FailedIds}");
            }
        }
    }
}
