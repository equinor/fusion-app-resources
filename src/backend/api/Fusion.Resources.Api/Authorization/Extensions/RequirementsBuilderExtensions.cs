using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Integration;
using Fusion.Integration.Profile;
using Fusion.Resources.Api.Authorization;
using Fusion.Resources.Api.Authorization.Requirements;
using Fusion.Resources.Domain;
using Microsoft.AspNetCore.Authorization;
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

        public static IAuthorizationRequirementRule OrgChartPositionWriteAccess(this IAuthorizationRequirementRule builder, Guid orgProjectId, Guid orgPositionId)
        {
            return builder.AddRule(OrgPositionAccessRequirement.OrgPositionWrite(orgProjectId, orgPositionId));
        }
        public static IAuthorizationRequirementRule OrgChartPositionReadAccess(this IAuthorizationRequirementRule builder, Guid orgProjectId, Guid orgPositionId)
        {
            return builder.AddRule(OrgPositionAccessRequirement.OrgPositionRead(orgProjectId, orgPositionId));
        }

        /// <summary>
        /// Indicates that the user is in any way or form a resource owner
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IAuthorizationRequirementRule IsResourceOwner(this IAuthorizationRequirementRule builder)
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAssertion(c => c.User.HasClaim(c => c.Type == FusionClaimsTypes.ResourceOwner))
                .Build();

            builder.AddRule((auth, user) => auth.AuthorizeAsync(user, policy));

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

        public static IAuthorizationRequirementRule CurrentUserIs(this IAuthorizationRequirementRule builder, PersonIdentifier personIdentifier)
        {          
            builder.AddRule(new CurrentUserIsRequirement(personIdentifier));
            return builder;
        }

        public static IAuthorizationRequirementRule BeContractorInProject(this IAuthorizationRequirementRule builder, ProjectIdentifier project)
        {
            builder.AddRule(project, new ContractorInProjectRequirement());
            return builder;
        }

        public static IAuthorizationRequirementRule BeContractorInContract(this IAuthorizationRequirementRule builder, Guid orgContractId)
        {
            builder.AddRule(new ContractorInContractRequirement(orgContractId));
            return builder;
        }

        public static IAuthorizationRequirementRule ProjectAccess(this IAuthorizationRequirementRule builder, ProjectAccess level, ProjectIdentifier project)
        {
            builder.AddRule(project, level);
            return builder;
        }
        public static IAuthorizationRequirementRule ScopeAccess(this IAuthorizationRequirementRule builder, ScopeAccess level)
        {
            builder.AddRule(level);
            return builder;
        }

        public static IAuthorizationRequirementRule ContractAccess(this IAuthorizationRequirementRule builder, ContractRole role, ProjectIdentifier project, Guid contractOrgId)
        {
            var resource = new ContractResource(project, contractOrgId);
            builder.AddRule(resource, role);

            return builder;
        }

        public static IAuthorizationRequirementRule DelegatedContractAccess(this IAuthorizationRequirementRule builder, DelegatedContractRole role, ProjectIdentifier project, Guid contractOrgId)
        {
            var resource = new ContractResource(project, contractOrgId);
            builder.AddRule(resource, role);

            return builder;
        }

        public static IAuthorizationRequirementRule RequestAccess(this IAuthorizationRequirementRule builder, RequestAccess level, QueryPersonnelRequest request)
        {
            builder.AddRule(request, level);

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

        #region Domain rules

        public static IAuthorizationRequirementRule CanDelegateInternalRole(this IAuthorizationRequirementRule builder, ProjectIdentifier project, Guid contractOrgId)
        {
            return builder.ContractAccess(ContractRole.AnyInternalRole, project, contractOrgId);
        }
        public static IAuthorizationRequirementRule CanDelegateExternalRole(this IAuthorizationRequirementRule builder, ProjectIdentifier project, Guid contractOrgId)
        {
            return builder
                .ContractAccess(ContractRole.AnyInternalRole, project, contractOrgId)
                .ContractAccess(ContractRole.AnyExternalRole, project, contractOrgId);
        }

        #endregion
    }
}
