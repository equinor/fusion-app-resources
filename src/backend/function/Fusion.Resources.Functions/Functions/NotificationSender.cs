using Fusion.Resources.Functions.Functions.Notifications;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.Functions
{
    public class NotificationSender
    {
        private readonly RequestNotificationSender requestNotificationSender;

        public NotificationSender(RequestNotificationSender requestNotificationSender)
        {
            this.requestNotificationSender = requestNotificationSender;
        }

        [Singleton]
        [FunctionName("request-summary-notification")]
        public async Task SendRequestSummaryNotification([TimerTrigger("*/5 * * * *", RunOnStartup = false)] TimerInfo timer, ILogger log)
        {
            log.LogInformation("Request notification summary starting");

            await requestNotificationSender.ProcessNotificationsAsync();

            log.LogInformation("Request notification summary completed successfully");
        }
    }
}
