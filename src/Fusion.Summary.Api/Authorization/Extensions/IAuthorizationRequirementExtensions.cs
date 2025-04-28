using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Summary.Api.Authorization.Requirements;
using Microsoft.AspNetCore.Authorization;

namespace Fusion.Summary.Api.Authorization.Extensions;

public static class IAuthorizationRequirementExtensions
{
    /// <summary>
    ///     Require that the user is a resource owner.
    ///     The check uses the resource owner claims in the user profile.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         To include additional local adjustments a local claims transformer can be used to add new claims.
    ///         Type="http://schemas.fusion.equinor.com/identity/claims/resourceowner" value="MY DEP PATH"
    ///     </para>
    ///     <para>
    ///         The parents check will only work for the direct path. Other resource owners in sibling departments of a parent
    ///         will not have access.
    ///         Ex. Check "L1 L2.1 L3.1 L4.1", owner in L2.1 L3.1, L2.1, L1 will have access, but ex. L2.2 will not have.
    ///     </para>
    /// </remarks>
    /// <param name="builder"></param>
    /// <param name="sapId">Org unit sapId</param>
    /// <param name="includeParents">Should resource owners in any of the direct parent departments have access</param>
    /// <param name="includeDescendants">Should anyone that is a resource owner in any of the sub departments have access</param>
    public static IAuthorizationRequirementRule BeResourceOwnerForDepartment(this IAuthorizationRequirementRule builder, string sapId, bool includeParents = false, bool includeDescendants = false)
    {
        builder.AddRule(new BeResourceOwnerForSapIdRequirement(sapId, includeParents, includeDescendants));
        return builder;
    }

    public static IAuthorizationRequirementRule ResourcesFullControl(this IAuthorizationRequirementRule builder)
    {
        var policy = new AuthorizationPolicyBuilder()
            .RequireAssertion(c => c.User.IsInRole("Fusion.Resources.FullControl"))
            .Build();

        builder.AddRule((auth, user) => auth.AuthorizeAsync(user, policy));

        return builder;
    }
}