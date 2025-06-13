using Fusion.Authorization;
using Fusion.Summary.Api.Domain.Models;
using Microsoft.AspNetCore.Authorization;

namespace Fusion.Summary.Api.Authorization.Requirements;

public class BeSiblingResourceOwnerRequirement : FusionAuthorizationRequirement, IAuthorizationHandler
{
    public BeSiblingResourceOwnerRequirement(QueryDepartment department, bool includeDelegatedResourceOwners)
    {
        Department = department;
        IncludeDelegatedResourceOwners = includeDelegatedResourceOwners;
    }

    public QueryDepartment Department { get; }
    public bool IncludeDelegatedResourceOwners { get; }

    public override string Description => ToString();
    public override string Code => "SiblingResourceOwner";

    public override string ToString()
    {
        var siblingText = IncludeDelegatedResourceOwners ? " or delegated resource owner" : string.Empty;

        return $"User is a sibling resource owner{siblingText} for the department '{Department.FullDepartmentName}'";
    }

    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        var departments = context.User.GetResponsibleForDepartments();
        if (!departments.Any())
        {
            SetEvaluation("User is not resource owner in any departments");
            return Task.CompletedTask;
        }

        var path = Department.FullDepartmentName.Trim().Split(" ");
        var parent = string.Join(" ", path.SkipLast(1));

        var siblingResponsibility = departments.Where(dep => dep.StartsWith(parent) && dep.Trim().Split(" ").Length == path.Length);
        if (siblingResponsibility.Any())
        {
            SetEvaluation($"User has access through responsibility in ${string.Join(", ", siblingResponsibility)}");
            context.Succeed(this);
        }
        else
        {
            SetEvaluation($"User has responsibility in departments: {string.Join(", ", departments)}; But not in a sibling of '{Department.FullDepartmentName}'");
        }
        return Task.CompletedTask;
    }
}
