using Fusion.Resources.Functions.Functions.Notifications;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.Functions
{
    public class RequestSummaryNotification
    {
        private readonly RequestNotificationSender requestNotificationSender;

        public RequestSummaryNotification(RequestNotificationSender requestNotificationSender)
        {
            this.requestNotificationSender = requestNotificationSender;
        }


        [FunctionName("request-summary-notification")]
        public async Task SendRequestSummaryNotification([TimerTrigger("*/5 * * * *", RunOnStartup = true)] TimerInfo timer, ILogger log)
        {
            log.LogInformation("Request notification summary starting");

            await requestNotificationSender.ProcessNotificationsAsync();

            log.LogInformation("Request notification summary completed successfully");
        }
    }
}
