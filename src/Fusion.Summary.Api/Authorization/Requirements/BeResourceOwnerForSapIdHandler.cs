using Fusion.Integration.LineOrg;
using Microsoft.AspNetCore.Authorization;

namespace Fusion.Summary.Api.Authorization.Requirements;

public class BeResourceOwnerForSapIdRequirementHandler : AuthorizationHandler<BeResourceOwnerForSapIdRequirement>
{
    private readonly ILineOrgResolver lineOrgResolver;

    public BeResourceOwnerForSapIdRequirementHandler(ILineOrgResolver lineOrgResolver)
    {
        this.lineOrgResolver = lineOrgResolver;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, BeResourceOwnerForSapIdRequirement requirement)
    {
        if (string.IsNullOrWhiteSpace(requirement.DepartmentSapId))
            return;

        var departmentSapId = requirement.DepartmentSapId;
        var includeParents = requirement.IncludeParents;
        var includeDescendants = requirement.IncludeDescendants;

        var apiOrgUnit = await lineOrgResolver.ResolveOrgUnitAsync(DepartmentId.FromSapId(departmentSapId));

        if (apiOrgUnit is null)
        {
            requirement.SetEvaluation($"Department with sapId '{departmentSapId}' not found");
            return;
        }

        var departmentFullPath = apiOrgUnit.FullDepartment;

        if (string.IsNullOrWhiteSpace(departmentFullPath))
        {
            requirement.SetEvaluation($"Department with sapId '{departmentSapId}' has no full path");
            return;
        }


        var departments = context.User
            .FindAll("Fusion.Resources.ResourceOwnerForDepartment")
            .Select(c => c.Value)
            .ToList();

        if (!departments.Any())
        {
            requirement.SetEvaluation("User is not resource owner in any departments");
            return;
        }


        // responsibility descendant Descendants
        var directResponsibility = departments.Any(d => d.Equals(departmentFullPath, StringComparison.OrdinalIgnoreCase));
        var descendantResponsibility = departments.Any(d => d.StartsWith(departmentFullPath, StringComparison.OrdinalIgnoreCase));
        var parentResponsibility = departments.Any(d => departmentFullPath.StartsWith(d, StringComparison.OrdinalIgnoreCase));

        var hasAccess = directResponsibility
                        || includeParents && parentResponsibility
                        || includeDescendants && descendantResponsibility;

        if (hasAccess)
        {
            requirement.SetEvaluation($"User has access though responsibility in {string.Join(", ", departments)}. " +
                                      $"[owner in department={directResponsibility}, parents={parentResponsibility}, descendants={descendantResponsibility}]");

            context.Succeed(requirement);
        }

        requirement.SetEvaluation($"User have responsibility in departments: {string.Join(", ", departments)}; But not in the requirement '{departmentSapId}'");
    }
}