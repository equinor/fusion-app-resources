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
            await requestNotificationSender.ProcessNotificationsAsync();
        }

        [Singleton]
        [FunctionName("sent-notifications-cleanup")]
        public async Task CleanupSentNotifications([TimerTrigger("0 0 0 * * *", RunOnStartup = false)] TimerInfo timer, ILogger log)
        {
            var dateCutoff = DateTime.Today.AddDays(-1);

            await sentNotificationsTable.CleanupSentNotifications(dateCutoff);
        }
    }
}
