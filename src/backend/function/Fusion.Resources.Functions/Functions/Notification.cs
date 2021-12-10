using Fusion.Resources.Functions.Notifications;
using Microsoft.Azure.Functions.Worker;
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

        [Function("request-summary-notification")]
        public async Task SendRequestSummaryNotification([TimerTrigger("*/5 * * * *", RunOnStartup = false)] TimerInfo timer)
        {
            await requestNotificationSender.ProcessNotificationsAsync();
        }

        
        [Function("sent-notifications-cleanup")]
        public async Task CleanupSentNotifications([TimerTrigger("0 0 0 * * *", RunOnStartup = false)] TimerInfo timer)
        {
            var dateCutoff = DateTime.Today.AddDays(-1);

            await sentNotificationsTable.CleanupSentNotifications(dateCutoff);
        }
    }
}
