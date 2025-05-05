using Fusion.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace Fusion.Summary.Api.Authorization.Requirements;

public class HaveGlobalRoleRequirement : FusionAuthorizationRequirement, IAuthorizationHandler
{
    public HaveGlobalRoleRequirement(string role)
    {
        Role = role;
    }

    public string Role { get; }
    public override string Description => $"You must have the role {Role}";
    public override string Code => "Role";


    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        if (!context.User.IsInRole(Role))
        {
            SetEvaluation($"You do not have sufficient permissions. In order to perform the operation you need the role: {Role}");
            return Task.CompletedTask;
        }

        context.Succeed(this);
        return Task.CompletedTask;
    }
}