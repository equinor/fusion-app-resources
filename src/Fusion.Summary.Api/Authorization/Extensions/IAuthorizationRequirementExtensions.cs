﻿using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Summary.Api.Authorization.Requirements;

namespace Fusion.Summary.Api.Authorization.Extensions;

public static class IAuthorizationRequirementExtensions
{

    public static IAuthorizationRequirementRule ResourcesFullControl(this IAuthorizationRequirementRule builder)
    {
        builder.AddRule(new HaveGlobalRoleRequirement(AccessRoles.ResourcesFullControl));

        return builder;
    }

    /// <summary>
    /// Require that the user is a resource owner.
    /// The check uses the resource owner claims in the user profile.
    /// </summary>
    /// <remarks>
    /// <para>
    /// To include additional local adjustments a local claims transformer can be used to add new claims.
    /// Type="http://schemas.fusion.equinor.com/identity/claims/resourceowner" value="MY DEP PATH"
    /// </para>
    /// <para>
    /// The parents check will only work for the direct path. Other resource owners in sibling departments of a parent will not have access.
    /// Ex. Check "L1 L2.1 L3.1 L4.1", owner in L2.1 L3.1, L2.1, L1 will have access, but ex. L2.2 will not have.
    /// </para>
    /// </remarks>
    /// <param name="builder">The requirement builder to add this requirement to.</param>
    /// <param name="department">The full department path access is being checked for</param>
    /// <param name="includeParents">Should resource owners in any of the direct parent departments have access</param>
    /// <param name="includeDescendants">Should anyone that is a resource owner in any of the sub departments have access</param>
    /// <param name="includeDelegatedResourceOwners">Should delegate resources owners be included/valid</param>
    public static IAuthorizationRequirementRule BeResourceOwnerForDepartment(this IAuthorizationRequirementRule builder,
        string department, bool includeParents = false, bool includeDescendants = false, bool includeDelegatedResourceOwners = false)
    {
        builder.AddRule(new BeResourceOwnerRequirement(department, includeParents, includeDescendants, includeDelegatedResourceOwners));
        return builder;
    }

    /// <summary>
    ///     Require that the user is a resource owner in a sibling department. A sibling department is a department that shares
    ///     the same parent department.
    /// </summary>
    /// <remarks>
    ///     Example:
    ///     <para>
    ///         <b>JKI PMM</b> CDA has sister/sibling departments <b>JKI PMM</b> KDI and <b>JKI PMM</b> POC
    ///     </para>
    /// </remarks>
    /// <param name="builder">The requirement builder to add this requirement to.</param>
    /// <param name="department">The department to check against</param>
    /// <param name="includeDelegatedResourceOwners">Should delegate resources owners be included</param>
    public static IAuthorizationRequirementRule BeSiblingResourceOwner(this IAuthorizationRequirementRule builder,
        string department, bool includeDelegatedResourceOwners = false)
    {
        builder.AddRule(new BeSiblingResourceOwnerRequirement(department, includeDelegatedResourceOwners));
        return builder;
    }
}
