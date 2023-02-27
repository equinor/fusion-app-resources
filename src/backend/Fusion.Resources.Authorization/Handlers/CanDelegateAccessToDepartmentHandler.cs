using System;
using System.Linq;
using Fusion.Resources.Api.Authorization.Requirements;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Authorization.Handlers
{
    public class CanDelegateAccessToDepartmentHandler : AuthorizationHandler<CanDelegateAccessToDepartmentRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CanDelegateAccessToDepartmentRequirement requirement)
        {
            var isResponsibleForDepartment = context.User.FindAll(ResourcesClaimTypes.ResourceOwnerForDepartment).Select(c => c.Value).FirstOrDefault();

            if (isResponsibleForDepartment != null && requirement.Department.StartsWith(isResponsibleForDepartment, StringComparison.OrdinalIgnoreCase))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            requirement.SetEvaluation(isResponsibleForDepartment != null
                ? $"User is resource owner for department '{isResponsibleForDepartment}', but do not have access to delegate access for department '{requirement.Department}'"
                : $"User is not resource owner for any department, and cannot delegate access for department '{requirement.Department}'");

            return Task.CompletedTask;
        }
    }
}
