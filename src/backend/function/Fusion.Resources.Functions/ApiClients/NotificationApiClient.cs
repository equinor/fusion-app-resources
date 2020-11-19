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

        public async Task<bool> PostNewNotificationAsync(Guid recipientAzureId, string title, string bodyMarkdown, INotificationApiClient.EmailPriority priority = INotificationApiClient.EmailPriority.Default)
        {
            var notification = new
            {
                AppKey = "resources",
                Priority = priority,
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

            var delaySettings = JsonConvert.DeserializeAnonymousType(body, new { Delay = 0 });

            return delaySettings.Delay;
        }
    }
}
