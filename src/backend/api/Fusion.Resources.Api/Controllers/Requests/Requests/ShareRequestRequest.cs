using FluentValidation;
using Fusion.Integration.Profile;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands.Requests.Sharing;
using Fusion.Resources.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Resources.Api.Controllers.Requests.Requests
{
    public class ShareRequestRequest
    {
        public string? Scope { get; set; }
        public string? Reason { get; set; }
        public List<PersonReference> SharedWith { get; set; }

        public ShareRequest ToCommand(Guid requestId)
        {
            if (string.IsNullOrEmpty(Scope)) Scope = SharedRequestScopes.BasicRead;

            var command = new ShareRequest(requestId, Scope, "User", Reason);
            command.SharedWith.AddRange(SharedWith.Select(x => (PersonId)x));

            return command;
        }

        public class Validator : AbstractValidator<ShareRequestRequest>
        {
            private List<PersonIdentifier> failedProfiles = new();
            public Validator(IProfileService profileService)
            {
                RuleFor(r => r.Scope).MaximumLength(100);
                RuleFor(r => r.Reason).MaximumLength(1000);

                RuleFor(x => x.SharedWith)
                    .NotEmpty()
                    .Must(x => x.Count <= 50).WithMessage("Requests can only be shared with up to 50 persons at once.")
                    .MustAsync(async (sharedWith, cancelToken) =>
                    {
                        var resolvedProfiles = await profileService.ResolveProfilesAsync(sharedWith.Select(x => (PersonId)x));
                        if (resolvedProfiles == null) return false;

                        bool isAllResolved = true;
                        foreach (var resolved in resolvedProfiles)
                        {
                            if (!resolved.Success)
                            {
                                failedProfiles.Add(resolved.Identifier);
                                isAllResolved = false;
                            }
                        }

                        return isAllResolved;
                    }).WithMessage($"Unable to resolve the following persons: {string.Join(", ", failedProfiles.Select(x => x.Mail))}");
            }
        }
    }
}
