using Fusion.Resources.Functions.Notifications;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions
{
    public class Notification
    {
        private readonly RequestNotificationSender requestNotificationSender;
        private readonly ISentNotificationsTableClient sentNotificationsTable;

        public Notification(RequestNotificationSender requestNotificationSender, ISentNotificationsTableClient sentNotificationsTable)
        {
            this.requestNotificationSender = requestNotificationSender;
            this.sentNotificationsTable = sentNotificationsTable;
        }

        [Singleton]
        [FunctionName("request-summary-notification")]
        public async Task SendRequestSummaryNotification([TimerTrigger("*/5 * * * *", RunOnStartup = false)] TimerInfo timer, ILogger log)
        {
            log.LogInformation("Request notification summary starting");

            await requestNotificationSender.ProcessNotificationsAsync();

            log.LogInformation("Request notification summary completed successfully");
        }

        [Singleton]
        [FunctionName("sent-notifications-cleanup")]
        public async Task CleanupSentNotifications([TimerTrigger("0 0 0 * * *", RunOnStartup = true)] TimerInfo timer, ILogger log)
        {
            log.LogInformation("Notification cleanup started");

            var dateCutoff = DateTime.Today.AddDays(-1);

            await sentNotificationsTable.CleanupSentNotifications(dateCutoff);

            log.LogInformation("Notification cleanup completed successfully");
        }
    }
}
