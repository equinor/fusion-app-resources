using Fusion.Authorization;
using Fusion.Summary.Api.Domain.Models;
using Microsoft.AspNetCore.Authorization;

namespace Fusion.Summary.Api.Authorization.Requirements;

public class BeParentResourceOwnerRequirement : FusionAuthorizationRequirement, IAuthorizationHandler
{
    public BeParentResourceOwnerRequirement(QueryDepartment department, bool includeDelegatedResourceOwners)
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
        var parentText = IncludeDelegatedResourceOwners ? " or delegated resource owner" : string.Empty;

        return $"User is a parent resource owner{parentText} for the department '{Department.FullDepartmentName}'";
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

        var parentResponsibility = departments.Where(dep => dep.Trim().Equals(parent, StringComparison.OrdinalIgnoreCase));
        if (parentResponsibility.Any())
        {
            SetEvaluation($"User has access through responsibility in ${string.Join(", ", parentResponsibility)}");
            context.Succeed(this);
        }
        else
        {
            SetEvaluation($"User has responsibility in departments: {string.Join(", ", departments)}; But not in the requirement '{parent}'");
        }
        return Task.CompletedTask;
    }
}
