namespace Fusion.Resources.Functions.Common.ApiClients.ApiModels;

public class ApiContext
{
    public Guid Id { get; set; }

    public string? ExternalId { get; set; }

    public ApiContextType Type { get; set; } = null!;

    public Dictionary<string, object?> Value { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Source { get; set; }

    public bool IsActive { get; set; }
}

public class ApiContextType
{
    public string Id { get; set; } = null!;

    public bool IsChildType { get; set; }

    public string[]? ParentTypeIds { get; set; }
}