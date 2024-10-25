using Fusion.Services.LineOrg.ApiModels;
using Newtonsoft.Json;

namespace Fusion.Resources.Functions.Common.ApiClients.ApiModels;

public class ApiSinglePersonRole
{
    public Guid Id { get; init; }

    public string RoleName { get; init; } = null!;

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? Identifier { get; set; }

    public string? Source { get; init; }

    public ApiSingleRoleScope Scope { get; init; } = null!;

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
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