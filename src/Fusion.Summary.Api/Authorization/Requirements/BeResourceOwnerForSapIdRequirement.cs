using Fusion.Authorization;

namespace Fusion.Summary.Api.Authorization.Requirements;

public class BeResourceOwnerForSapIdRequirement : FusionAuthorizationRequirement
{
    public BeResourceOwnerForSapIdRequirement(string departmentSapId, bool includeParents = false, bool includeDescendants = false)
    {
        DepartmentSapId = departmentSapId;
        IncludeParents = includeParents;
        IncludeDescendants = includeDescendants;
    }

    public override string Description => ToString();

    public override string Code => "ResourceOwner";

    public string? DepartmentSapId { get; }
    public bool IncludeParents { get; }
    public bool IncludeDescendants { get; }

    public override string ToString()
    {
        if (string.IsNullOrEmpty(DepartmentSapId))
            return "User must be resource owner of a department";

        if (IncludeParents && IncludeDescendants)
            return $"User must be resource owner in department '{DepartmentSapId}' or any departments above or below";

        if (IncludeParents)
            return $"User must be resource owner in department '{DepartmentSapId}' or any departments above";

        if (IncludeDescendants)
            return $"User must be resource owner in department '{DepartmentSapId}' or any sub departments";

        return $"User must be resource owner in department '{DepartmentSapId}'";
    }
}