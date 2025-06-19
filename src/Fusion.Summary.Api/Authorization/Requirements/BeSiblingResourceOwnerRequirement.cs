using Fusion.Authorization;
using Fusion.Summary.Domain;
using Microsoft.AspNetCore.Authorization;

namespace Fusion.Summary.Api.Authorization.Requirements;

public class BeSiblingResourceOwnerRequirement : FusionAuthorizationRequirement, IAuthorizationHandler
{
    public BeSiblingResourceOwnerRequirement(string department, bool includeDelegatedResourceOwners)
    {
        DepartmentPath = new(department);
        IncludeDelegatedResourceOwners = includeDelegatedResourceOwners;
    }

    public DepartmentPath DepartmentPath { get; }
    public bool IncludeDelegatedResourceOwners { get; }

    public override string Description => ToString();
    public override string Code => "SiblingResourceOwner";

    public override string ToString()
    {
        var siblingText = IncludeDelegatedResourceOwners ? " or delegated resource owner" : string.Empty;

        return $"User is a sibling resource owner{siblingText} for the department '{DepartmentPath}'";
    }

    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        var resourceOwnerForDepartments = context.User.FindAll(ResourcesClaimTypes.ResourceOwnerForDepartment)
            .Select(c => c.Value)
            .ToList();

        if (IncludeDelegatedResourceOwners)
        {
            var delegatedDepartments = context.User.FindAll(ResourcesClaimTypes.DelegatedResourceOwnerForDepartment)
                .Select(c => c.Value)
                .ToList();
            resourceOwnerForDepartments.AddRange(delegatedDepartments);
        }

        if (resourceOwnerForDepartments.Count == 0)
        {
            SetEvaluation("User is not a resource owner in any departments");
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(DepartmentPath))
        {
            return Task.CompletedTask;
        }

        var resourceParent = DepartmentPath.ParentDepartment;

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