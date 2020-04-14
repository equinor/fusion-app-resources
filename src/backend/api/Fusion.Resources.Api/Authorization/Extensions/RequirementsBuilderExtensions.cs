using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Integration;
using Fusion.Resources.Api.Authorization;
using Fusion.Resources.Domain;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;


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
            var policy = new AuthorizationPolicyBuilder()
                .RequireAssertion(ctx =>
                {
                    var contractProjectIds = ctx.User.Claims.Where(c => c.Type == FusionClaimsTypes.FusionContract && c.Properties.ContainsKey("projectId"))
                        .Select(c => { Guid.TryParse(c.Properties["projectId"], out Guid projectId); return projectId; });

                    return contractProjectIds.Any(pid => pid == project.ProjectId);

                }).Build();

            builder.AddRule((auth, user) => auth.AuthorizeAsync(user, policy));

            return builder;
        }

        public static IAuthorizationRequirementRule BeContractorInContract(this IAuthorizationRequirementRule builder, Guid orgContractId)
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAssertion(ctx =>
                {
                    var contractContractIds = ctx.User.Claims.Where(c => c.Type == FusionClaimsTypes.FusionContract && c.Properties.ContainsKey("contractId"))
                        .Select(c => { Guid.TryParse(c.Properties["contractId"], out Guid projectId); return projectId; });

                    return contractContractIds.Any(cid => cid == orgContractId);

                }).Build();

            builder.AddRule((auth, user) => auth.AuthorizeAsync(user, policy));

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
    }
}
