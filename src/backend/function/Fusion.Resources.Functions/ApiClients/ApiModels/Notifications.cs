using System;
using Newtonsoft.Json;

namespace Fusion.Resources.Functions.ApiClients.ApiModels;

public class SendNotificationsRequest
{
    [JsonProperty("emailPriority")] public int EmailPriority { get; set; }

    [JsonProperty("title")] public string Title { get; set; }

    [JsonProperty("description")] public string Description { get; set; }

    [JsonProperty("card")] public object Card { get; set; }
}
