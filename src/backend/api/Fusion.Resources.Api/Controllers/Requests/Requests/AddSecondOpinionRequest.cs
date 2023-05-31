using FluentValidation;
using Fusion.Resources.Domain;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Resources.Api.Controllers
{
    public class AddSecondOpinionRequest
    {
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public List<PersonReference> AssignedTo { get; set; } = null!;

        public class Validator : AbstractValidator<AddSecondOpinionRequest>
        {
            public Validator(IServiceProvider services)
            {
                RuleFor(x => x.Title).NotEmpty();
                RuleFor(x => x.Description).NotEmpty();
                RuleFor(x => x.AssignedTo).NotEmpty();

                RuleFor(x => x.AssignedTo)
                    .Must((req, assignedToIds, context) =>
                    {
                        
                        var profileResolver = services.GetRequiredService<IProfileService>();

                        // Need to wait here, because aspnet validation pipeline is not async. 
                        // Ref: https://docs.fluentvalidation.net/en/latest/aspnet.html
                        var resolved = profileResolver.ResolveProfilesAsync(assignedToIds.Select(x => (PersonId)x)).Result;

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
