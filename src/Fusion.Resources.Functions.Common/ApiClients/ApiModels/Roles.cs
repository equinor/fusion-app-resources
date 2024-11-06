
namespace Fusion.Resources.Functions.Common.ApiClients.ApiModels;

public class ApiSinglePersonRole
{
    public ApiSingleRoleScope Scope { get; init; } = null!;

    public ApiPerson? Person { get; init; }

    public DateTimeOffset? ValidTo { get; init; }
}

public class ApiSingleRoleScope
{
    public ApiSingleRoleScope(string type, string value)
    {
        Type = type;
        Value = value;
    }

    public string Type { get; init; }
    public string Value { get; init; }
}

public class ApiPerson
{
    public Guid Id { get; init; }
    public string? Mail { get; init; }
}