using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.ApiClients
{
    internal class NotificationApiClient : INotificationApiClient
    {
        private readonly HttpClient notificationsClient;
        private readonly ILogger<NotificationApiClient> log;

        public NotificationApiClient(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
        {
            notificationsClient = httpClientFactory.CreateClient(HttpClientNames.Application.Notifications);
            log = loggerFactory.CreateLogger<NotificationApiClient>();
        }

        public async Task<bool> PostNewNotificationAsync(Guid recipientAzureId, string title, string bodyMarkdown)
        {
            var notification = new
            {
                AppKey = "resources",
                Priority = "Default",
                Title = title,
                Description = bodyMarkdown
            };

            var response = await notificationsClient.PostAsJsonAsync($"persons/{recipientAzureId}/notifications", notification);

            if (!response.IsSuccessStatusCode)
            {
                log.LogWarning($"Failed to notify recipient with Azure ID '{recipientAzureId}': [{response.StatusCode}]");
                return false;
            }

            return true;
        }

        public async Task<int> GetDelayForUserAsync(Guid azureUniqueId)
        {
            var settingsResponse = await notificationsClient.GetAsync($"persons/{azureUniqueId}/notifications/settings");
            var body = await settingsResponse.Content.ReadAsStringAsync();

            if (!settingsResponse.IsSuccessStatusCode)
            {
                log.LogWarning($"Unable to retrieve settings for '{azureUniqueId}'. Defaulting to 60 minutes");
                return 60;
            }

            var bodyJson = JsonConvert.DeserializeAnonymousType(body, new { Delay = 0 });
            int delay = Math.Max(bodyJson.Delay, 60); //minimum frequency is 60 minutes, otherwise as-configured.

            return delay;
        }
    }
}
