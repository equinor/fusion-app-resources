using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
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

        public async Task<bool> PostNewNotificationAsync(Guid recipientAzureId, string title, string bodyMarkdown, INotificationApiClient.Priority priority = INotificationApiClient.Priority.Default)
        {
            var notification = new
            {
                AppKey = "resources",
                EmailPriority = priority.ToString(),
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

        public async Task<INotificationApiClient.NotificationSettings> GetSettingsForUser(Guid azureUniqueId)
        {
            var settingsResponse = await notificationsClient.GetAsync($"persons/{azureUniqueId}/notifications/settings");
            var body = await settingsResponse.Content.ReadAsStringAsync();

            if (!settingsResponse.IsSuccessStatusCode)
            {
                log.LogWarning($"Unable to retrieve settings for '{azureUniqueId}'. Using default settings (enabled, 60 min delay)");
                return new INotificationApiClient.NotificationSettings(true, 60, true);
            }

            var settings = JsonConvert.DeserializeAnonymousType(body, new
            {
                Email = false,
                DelayInMinutes = 0,
                AppConfig = new[]
                {
                    new { AppKey = string.Empty, Enabled = false }
                }
            });

            return new INotificationApiClient.NotificationSettings(settings.Email, settings.DelayInMinutes, settings.AppConfig.Any(app => app.AppKey == "resources" && app.Enabled));
        }
    }
}
