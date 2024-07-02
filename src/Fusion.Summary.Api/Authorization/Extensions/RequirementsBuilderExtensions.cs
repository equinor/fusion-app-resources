using Fusion.AspNetCore.FluentAuthorization;
using Microsoft.AspNetCore.Authorization;

namespace Fusion.Summary.Api.Authorization.Extensions;

public static class RequirementsBuilderExtensions
{
    public static IAuthorizationRequirementRule FullControl(this IAuthorizationRequirementRule builder)
    {
        var policy = new AuthorizationPolicyBuilder()
            .RequireAssertion(c => c.User.IsInRole("Fusion.Resources.FullControl"))
            .Build();

        builder.AddRule((auth, user) => auth.AuthorizeAsync(user, policy));

        return builder;
    }
}