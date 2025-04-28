using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Summary.Api.Authorization.Requirements;

namespace Fusion.Summary.Api.Authorization.Extensions;

public static class IAuthorizationRequirementExtensions
{

    public static IAuthorizationRequirementRule ResourcesFullControl(this IAuthorizationRequirementRule builder)
    {
        builder.AddRule(new HaveGlobalRoleRequirement(AccessRoles.ResourcesFullControl));

        return builder;
    }
}