using System;
using System.Linq;
using Fusion.Resources.Api.Authorization.Requirements;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Authorization.Handlers
{
    public class CanDelegateAccessToDepartmentHandler : AuthorizationHandler<CanDelegateAccessToDepartmentRequirement>
    {
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CanDelegateAccessToDepartmentRequirement requirement)
        {
            var isResponsibleForDepartment = context.User.FindAll(ResourcesClaimTypes.ResourceOwnerForDepartment).Select(c => c.Value).FirstOrDefault();

            if (isResponsibleForDepartment != null && requirement.Department.StartsWith(isResponsibleForDepartment, StringComparison.OrdinalIgnoreCase))
            {
                context.Succeed(requirement);
                return;
            }

            requirement.SetEvaluation($"User is resource owner for {isResponsibleForDepartment}, but do not have access to delegate access for {requirement.Department}");
        }
    }
}
