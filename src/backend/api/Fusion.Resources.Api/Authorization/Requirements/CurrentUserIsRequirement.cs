using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Integration.Profile;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Authorization
{
    public class CurrentUserIsRequirement : AuthorizationHandler<CurrentUserIsRequirement>, IReportableAuthorizationRequirement, IAuthorizationRequirement
    {
        public CurrentUserIsRequirement(PersonIdentifier personIdentifier)
        {
            PersonIdentifier = personIdentifier;
        }

        public string Description => "Current user is the same as the specified user";

        public string Code => "CurrentUserIs";

        public PersonIdentifier PersonIdentifier { get; }

        public string? Evaluation { get; set; }

        public bool IsEvaluated { get; set; }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CurrentUserIsRequirement requirement)
        {
            requirement.IsEvaluated = true;

            var userMail = context.User.GetUserMail();
            var azureUniqueId = context.User.GetAzureUniqueId();

            switch (PersonIdentifier.Type)
            {
                case IdentifierType.Mail:
                    if (string.Equals(userMail, PersonIdentifier.Mail, StringComparison.OrdinalIgnoreCase))
                        context.Succeed(this);
                    break;

                case IdentifierType.UniqueId:
                case IdentifierType.UniqueIdAndMail:
                    if (azureUniqueId.HasValue && azureUniqueId.Value == PersonIdentifier.AzureUniquePersonId)
                        context.Succeed(this);
                    break;
            }

            if (!context.HasSucceeded)
                Evaluation = $"Current user ({userMail} / {azureUniqueId}) does not match requirement {PersonIdentifier}";

            return Task.CompletedTask;
        }
    }
}
