using Fusion.Authorization;
using Fusion.Summary.Api.Domain.Models;
using Microsoft.AspNetCore.Authorization;

namespace Fusion.Summary.Api.Authorization.Requirements;

public class BeDirectDescendantResourceOwnerRequirement : FusionAuthorizationRequirement, IAuthorizationHandler
{
    public BeDirectDescendantResourceOwnerRequirement(QueryDepartment department, bool includeDelegatedResourceOwners)
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
        var descendantText = IncludeDelegatedResourceOwners ? " or delegated resource owner" : string.Empty;

        return $"User is a direct descendant resource owner{descendantText} for the department '{Department.FullDepartmentName}'";
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

        var descendantResponsibility = departments.Where(dep =>
        {
            var trimmed = dep.Trim();
            return trimmed.StartsWith(Department.FullDepartmentName.Trim(), StringComparison.OrdinalIgnoreCase) &&
                trimmed.Split(" ").Length == path.Length + 1;
        });

        if (descendantResponsibility.Any())
        {
            SetEvaluation($"User has access through responsibility in ${string.Join(", ", descendantResponsibility)}");
            context.Succeed(this);
        }
        else
        {
            SetEvaluation($"User has responsibility in departments: {string.Join(", ", departments)}; But not a direct descendant of '{Department.FullDepartmentName}'");
        }
        return Task.CompletedTask;
    }
}
