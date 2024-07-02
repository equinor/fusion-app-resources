using System.Text;
using Fusion.Resources.Functions.Common.ApiClients.ApiModels;
using Fusion.Resources.Functions.Common.Integration.Http;
using Newtonsoft.Json;

namespace Fusion.Resources.Functions.Common.ApiClients;

public class NotificationApiClient : INotificationApiClient
{
    private readonly HttpClient _client;

    public NotificationApiClient(IHttpClientFactory httpClientFactory)
    {
        _client = httpClientFactory.CreateClient(HttpClientNames.Application.Notifications);
        _client.Timeout = TimeSpan.FromMinutes(5);
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