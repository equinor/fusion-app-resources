using System;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.ApiClients
{
    public interface INotificationApiClient
    {
        Task<NotificationSettings> GetSettingsForUser(Guid azureUniqueId);

        Task<bool> PostNewNotificationAsync(Guid recipientAzureId, string title, string bodyMarkdown, Priority priority = Priority.Default);

        public class NotificationSettings
        {
            public NotificationSettings(bool mailEnabled, int delay, bool resouresEnabled)
            {
                MailIsEnabled = mailEnabled;
                Delay = delay;
                ResourcesIsEnabled = resouresEnabled;
            }
            public bool ResourcesIsEnabled { get; set; }

            public bool MailIsEnabled { get; set; }

            public int Delay { get; set; }
        }

        public enum Priority
        {
            High,
            Default,
            Low
        }
    }
}