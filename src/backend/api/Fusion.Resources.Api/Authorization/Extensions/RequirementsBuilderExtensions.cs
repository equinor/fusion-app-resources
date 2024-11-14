using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Authorization;
using Fusion.Integration;
using Fusion.Integration.Profile;
using Fusion.Resources.Api.Authorization;
using Fusion.Resources.Api.Authorization.Requirements;
using Fusion.Resources.Authorization.Requirements;
using Fusion.Resources.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using System;
using System.Linq;


namespace Fusion.Resources.Api.Controllers
{
    public static class InternalRequestRequirementExtensions
    {
        public static IAuthorizationRequirementRule FullControlInternal(this IAuthorizationRequirementRule builder)
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAssertion(c => c.User.IsInRole("Fusion.Resources.Internal.FullControl"))
                .Build();

            builder.AddRule((auth, user) => auth.AuthorizeAsync(user, policy));

            return builder;
        }

        public static IAuthorizationRequirementRule FullControlExternal(this IAuthorizationRequirementRule builder)
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAssertion(c => c.User.IsInRole("Fusion.Resources.External.FullControl"))
                .Build();

            builder.AddRule((auth, user) => auth.AuthorizeAsync(user, policy));

            return builder;
        }

       public static IAuthorizationRequirementRule GlobalRoleAccess(this IAuthorizationRequirementRule builder, params string[] roles)
        {
            return builder.AddRule(new GlobalRoleRequirement(roles));
        }
        public static IAuthorizationRequirementRule AllGlobalRoleAccess(this IAuthorizationRequirementRule builder, params string[] roles)
        {
            return builder.AddRule(new GlobalRoleRequirement(GlobalRoleRequirement.RoleRequirement.All, roles));
        }
        public static IAuthorizationRequirementRule OrgChartPositionWriteAccess(this IAuthorizationRequirementRule builder, Guid orgProjectId, Guid orgPositionId)
        {
            return builder.AddRule(OrgPositionAccessRequirement.OrgPositionWrite(orgProjectId, orgPositionId));
        }
        public static IAuthorizationRequirementRule OrgChartPositionReadAccess(this IAuthorizationRequirementRule builder, Guid orgProjectId, Guid orgPositionId)
        {
            return builder.AddRule(OrgPositionAccessRequirement.OrgPositionRead(orgProjectId, orgPositionId));
        }
        public static IAuthorizationRequirementRule OrgChartReadAccess(this IAuthorizationRequirementRule builder, Guid orgProjectId)
        {
            return builder.AddRule(OrgProjectAccessRequirement.OrgRead(orgProjectId));
        }
        public static IAuthorizationRequirementRule OrgChartWriteAccess(this IAuthorizationRequirementRule builder, Guid orgProjectId)
        {
            return builder.AddRule(OrgProjectAccessRequirement.OrgWrite(orgProjectId));
        }

        public static IAuthorizationRequirementRule RequireConversationForTaskOwner(this IAuthorizationRequirementRule builder, QueryMessageRecipient recipient)
        {
            return builder.AddRule(new AssertionRequirement(_ => recipient == QueryMessageRecipient.TaskOwner));
        }
        public static IAuthorizationRequirementRule RequireConversationForResourceOwner(this IAuthorizationRequirementRule builder, QueryMessageRecipient recipient)
        {
            return builder.AddRule(new AssertionRequirement(_ => recipient == QueryMessageRecipient.ResourceOwner));
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
        /// <param name="builder"></param>
        /// <param name="departmentPath">The full department path</param>
        /// <param name="includeParents">Should resource owners in any of the direct parent departments have access</param>
        /// <param name="includeDescendants">Should anyone that is a resource owner in any of the sub departments have access</param>
        public static IAuthorizationRequirementRule BeResourceOwnerForDepartment(this IAuthorizationRequirementRule builder, string department, bool includeParents = false, bool includeDescendants = false)
        {
            builder.AddRule(new BeResourceOwnerRequirement(department, includeParents, includeDescendants));
            return builder;
        }

        /// <summary>
        /// Requires the user to be resource owner for any department
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IAuthorizationRequirementRule BeResourceOwnerForAnyDepartment(this IAuthorizationRequirementRule builder)
        {
            builder.AddRule(new BeResourceOwnerRequirement());
            return builder;
        }


        public static IAuthorizationRequirementRule HaveRole(this IAuthorizationRequirementRule builder, string role)
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireRole(role)
                .Build();
            return builder.AddRule((auth, user) => auth.AuthorizeAsync(user,policy));
        }

        public static IAuthorizationRequirementRule BeSiblingResourceOwner(this IAuthorizationRequirementRule builder, DepartmentPath path)
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAssertion(c =>
                {
                    // User has access if the parent department matches..
                    var resourceParent = path.ParentDeparment;
                    var userDepartments = c.User.GetManagerForDepartments();

                    return userDepartments.Any(d => resourceParent.IsDepartment(new DepartmentPath(d).Parent()));
                })
                .Build();

            builder.AddRule((auth, user) => auth.AuthorizeAsync(user, policy));

            return builder;
        }

        /// <summary>
        /// User has access if the user is resource owner for any department which has the specified department as parent.
        /// </summary>
        /// <returns></returns>
        public static IAuthorizationRequirementRule BeDirectChildResourceOwner(this IAuthorizationRequirementRule builder, DepartmentPath path)
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAssertion(c =>
                {
                    var userDepartments = c.User.GetManagerForDepartments()
                        .Select(d => new DepartmentPath(d).Parent());

                    return userDepartments.Any(d => path.IsDepartment(d));
                })
                .Build();

            builder.AddRule((auth, user) => auth.AuthorizeAsync(user, policy));

            return builder;
        }

        public static IAuthorizationRequirementRule HaveBasicRead(this IAuthorizationRequirementRule builder, Guid requestId)
        {
            builder.AddRule(new ClaimsAuthorizationRequirement(ResourcesClaimTypes.BasicRead, new[] { requestId.ToString() }));
            return builder;
        }
    }
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

        public static IAuthorizationRequirementRule ResourcesRead(this IAuthorizationRequirementRule builder)
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAssertion(c => c.User.IsInRole("Fusion.Resources.Read"))
                .Build();

            builder.AddRule((auth, user) => auth.AuthorizeAsync(user, policy));

            return builder;
        }

        public static IAuthorizationRequirementRule CurrentUserIs(this IAuthorizationRequirementRule builder, PersonIdentifier personIdentifier)
        {
            builder.AddRule(new CurrentUserIsRequirement(personIdentifier));
            return builder;
        }

        public static IAuthorizationRequirementRule ScopeAccess(this IAuthorizationRequirementRule builder, ScopeAccess level)
        {
            builder.AddRule(level);
            return builder;
        }

        public static IAuthorizationRequirementRule BeCommentAuthor(this IAuthorizationRequirementRule builder, QueryRequestComment comment)
        {
            builder.AddRule(comment, new RequestCommentAuthor());

            return builder;
        }

        public static IAuthorizationRequirementRule BeRequestCreator(this IAuthorizationRequirementRule builder, Guid requestId)
        {
            builder.AddRule(new RequestCreatorRequirement(requestId));
            return builder;
        }
        public static IAuthorizationRequirementRule CanDelegateAccessToDepartment(this IAuthorizationRequirementRule builder, string department)
        {
            builder.AddRule(new CanDelegateAccessToDepartmentRequirement(department));
            return builder;
        }

    }
}
