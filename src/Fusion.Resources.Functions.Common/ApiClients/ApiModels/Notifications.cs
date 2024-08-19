using AdaptiveCards;
using Newtonsoft.Json;

namespace Fusion.Resources.Functions.Common.ApiClients.ApiModels;

public class SendNotificationsRequest
{
    [JsonProperty("emailPriority")] public int EmailPriority { get; set; }

    [JsonProperty("title")] public string Title { get; set; }

    [JsonProperty("description")] public string Description { get; set; }

    [JsonProperty("appKey")] public string AppKey { get; set; }

    [JsonProperty("card")] public AdaptiveCard Card { get; set; }
}
