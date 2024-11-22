using Fusion.Integration.Core.Http.OData;
using Fusion.Services.Org.ApiModels;
using Newtonsoft.Json;

namespace Fusion.Resources.Functions.Common.ApiClients;

public interface IOrgClient
{
    Task<ApiChangeLog> GetChangeLog(string projectId, DateTime timestamp);

    Task<List<ApiProjectV2>> GetProjectsAsync(ODataQuery? query = null, CancellationToken cancellationToken = default);
    Task<ICollection<ApiPositionV2>> GetProjectPositions(string projectId, CancellationToken cancellationToken = default);
}

#region model

public class ApiChangeLog
{
    public Guid ProjectId { get; set; }
    public DateTimeOffset? FirstEventDate { get; set; }
    public DateTimeOffset? LastEventDate { get; set; }
    public List<ApiChangeLogEvent> Events { get; set; }
}

public class ApiChangeLogEvent
{
    public Guid? PositionId { get; set; }
    public string? PositionName { get; set; }
    public string? PositionExternalId { get; set; }
    public Guid? InstanceId { get; set; }
    public string Name { get; set; }
    public string ChangeCategory { get; set; }
    public ApiPersonV2? Actor { get; set; }
    public DateTimeOffset TimeStamp { get; set; }
    public string? Description { get; set; }
    public string ChangeType { get; set; }
    public object? PayLoad { get; set; }

    public ApiInstanceSnapshot? Instance { get; init; }

    public Guid EventId { get; set; }
    public string EventFriendlyName { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public Guid? DraftId { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? ChangeSource { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? ChangeSourceId { get; set; }

    public class ApiInstanceSnapshot
    {
        public DateTime? AppliesFrom { get; set; }
        public DateTime? AppliesTo { get; set; }
        public double? WorkLoad { get; set; }
    }
}

#endregion