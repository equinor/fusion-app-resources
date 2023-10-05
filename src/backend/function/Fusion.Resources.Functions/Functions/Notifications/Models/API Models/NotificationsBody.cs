using Newtonsoft.Json;

namespace Fusion.Resources.Functions.Functions.Notifications.Models.API_Models;

public class NotificationsBody
{
    [JsonProperty("appKey")] public string AppKey { get; set; }

    [JsonProperty("emailPriority")] public int EmailPriority { get; set; }

    [JsonProperty("fallbackHtml")] public string FallbackHtml { get; set; }

    [JsonProperty("title")] public string Title { get; set; }

    [JsonProperty("description")] public string Description { get; set; }

    [JsonProperty("card")] public string Card { get; set; }

    [JsonProperty("sourceSystem")] public SourceSystem SourceSystem { get; set; }

    [JsonProperty("originalCreatorUniqueId")]
    public string OriginalCreatorUniqueId { get; set; }
}

public class SourceSystem
{
    [JsonProperty("name")] public string Name { get; set; }

    [JsonProperty("subSystem")] public string SubSystem { get; set; }

    [JsonProperty("identifier")] public string Identifier { get; set; }
}