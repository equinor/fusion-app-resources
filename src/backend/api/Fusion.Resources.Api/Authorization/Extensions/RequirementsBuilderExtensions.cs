using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Resources.Api.Authorization;
using Fusion.Resources.Domain;
using Microsoft.AspNetCore.Authorization;
using System;


namespace Fusion.Resources.Api.Controllers
{
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
