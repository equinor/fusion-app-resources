using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fusion.Resources.Functions.ApiClients.ApiModels;
using Newtonsoft.Json;

namespace Fusion.Resources.Functions.ApiClients;

public class NotificationApiClient : INotificationApiClient
{
    private readonly HttpClient _client;

    public NotificationApiClient(IHttpClientFactory httpClientFactory)
    {
        _client = httpClientFactory.CreateClient(HttpClientNames.Application.Notifications);
    }

    public async Task<bool> SendNotification(SendNotificationsRequest request, Guid azureUniqueId)
    {
        var content = JsonConvert.SerializeObject(request);
        var stringContent = new StringContent(content, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync($"/persons/{azureUniqueId}/notifications?api-version=1.0",
            stringContent);

        return response.IsSuccessStatusCode;
    }
}