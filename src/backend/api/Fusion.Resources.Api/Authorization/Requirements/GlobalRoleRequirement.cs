using System.Linq;
using System.Threading.Tasks;
using Fusion.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace Fusion.Resources.Api.Authorization
{
    public class GlobalRoleRequirement : FusionAuthorizationRequirement, IAuthorizationHandler
    {
        public enum RoleRequirement { Any, All }
        public string[] Roles { get; }
        public RoleRequirement Type { get; }
        public override string Description => $"User must have [{Type}] of roles: {string.Join(", ", Roles)}";
        public override string Code => "GlobalRoles";
        public GlobalRoleRequirement(params string[] roles)
            : this(RoleRequirement.Any, roles)
        {
        }
        public GlobalRoleRequirement(RoleRequirement type, params string[] roles)
        {
            Roles = roles;
            Type = type;
        }
        public Task HandleAsync(AuthorizationHandlerContext context)
        {
            var isInRoles = Roles.Select(r => context.User.IsInRole(r));
            switch (Type)
            {
                case RoleRequirement.Any:
                    if (isInRoles.Any(b => b == true))
                        context.Succeed(this);
                    break;
                case RoleRequirement.All:
                    if (isInRoles.All(b => b == true))
                        context.Succeed(this);
                    break;
            }
            return Task.CompletedTask;
        }
    }
}