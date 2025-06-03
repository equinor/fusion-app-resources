using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Summary.Api.Authorization.Requirements;
using Fusion.Summary.Api.Domain.Models;

namespace Fusion.Summary.Api.Authorization.Extensions;

public static class IAuthorizationRequirementExtensions
{

    public static IAuthorizationRequirementRule ResourcesFullControl(this IAuthorizationRequirementRule builder)
    {
        builder.AddRule(new HaveGlobalRoleRequirement(AccessRoles.ResourcesFullControl));

        return builder;
    }

    public static IAuthorizationRequirementRule BeSiblingResourceOwner(this IAuthorizationRequirementRule builder,
        QueryDepartment department, bool includeDelegatedResourceOwners = false)
    {
        builder.AddRule(new BeSiblingResourceOwnerRequirement(department, includeDelegatedResourceOwners));
        return builder;
    }

    public static IAuthorizationRequirementRule BeParentResourceOwner(this IAuthorizationRequirementRule builder,
            QueryDepartment department, bool includeDelegatedResourceOwners = false)
    {
        builder.AddRule(new BeParentResourceOwnerRequirement(department, includeDelegatedResourceOwners));
        return builder;
    }

    public static IAuthorizationRequirementRule BeDirectDescendantResourceOwner(this IAuthorizationRequirementRule builder,
            QueryDepartment department, bool includeDelegatedResourceOwners = false)
    {
        builder.AddRule(new BeDirectDescendantResourceOwnerRequirement(department, includeDelegatedResourceOwners));
        return builder;
    }

}
