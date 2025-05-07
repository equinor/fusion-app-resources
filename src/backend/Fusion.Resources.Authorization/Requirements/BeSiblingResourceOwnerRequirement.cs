using System.Linq;
using System.Threading.Tasks;
using Fusion.Authorization;
using Fusion.Resources.Domain;
using Microsoft.AspNetCore.Authorization;

namespace Fusion.Resources.Authorization.Requirements;

public class BeSiblingResourceOwnerRequirement : FusionAuthorizationRequirement, IAuthorizationHandler
{
    public BeSiblingResourceOwnerRequirement(DepartmentPath departmentPath)
    {
        DepartmentPath = departmentPath;
    }

    public DepartmentPath DepartmentPath { get; }


    public override string Description => ToString();
    public override string Code => "SiblingResourceOwner";

    public override string ToString()
    {
        return "User is a sibling resource owner. Two departments are siblings if they share the same parent department.";
    }

    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        var resourceOwnerForDepartments = context.User.FindAll(ResourcesClaimTypes.ResourceOwnerForDepartment)
            .Select(c => c.Value)
            .ToArray();

        if (resourceOwnerForDepartments.Length == 0)
        {
            SetEvaluation("User is not resource owner in any departments");
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(DepartmentPath))
        {
            return Task.CompletedTask;
        }

        var resourceParent = DepartmentPath.ParentDeparment;

        // User has access if the parent department matches.
        if (resourceOwnerForDepartments.Any(d => resourceParent.IsDepartment(new DepartmentPath(d).Parent())))
        {
            SetEvaluation($"User is a resource owner for a direct child department of '{DepartmentPath.Parent()}'");
            context.Succeed(this);
            return Task.CompletedTask;
        }

        SetEvaluation($"User is not a resource owner for a direct child department of '{DepartmentPath.Parent()}'");
        return Task.CompletedTask;
    }
}