using Fusion.Services.LineOrg.ApiModels;

namespace Fusion.Resources.Functions.Common.ApiClients.ApiModels;

public class ApiSinglePersonRole
{
    public ApiSingleRoleScope Scope { get; init; } = null!;

    public ApiPerson? Person { get; set; }

    public DateTimeOffset? ValidTo { get; init; }
}

public class ApiSingleRoleScope
{
    public ApiSingleRoleScope(string type, string value)
    {
        Type = type;
        Value = value;
    }

    public string Type { get; set; }
    public string Value { get; set; }
}