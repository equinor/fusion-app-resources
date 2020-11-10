using Fusion.Integration.Notification;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.Functions
{
    public class RequestSummaryNotification
    {
        private readonly HttpClient resourcesClient;
        private readonly IFusionNotificationClient notificationClient;

        public RequestSummaryNotification(IFusionNotificationClient notificationClient, IHttpClientFactory httpClientFactory)
        {
            resourcesClient = httpClientFactory.CreateClient(HttpClientNames.Application.Resources);
            this.notificationClient = notificationClient;
        }

        [FunctionName("request-summary-notification")]
        public async Task SendRequestSummaryNotification([TimerTrigger("0 */5 * * *", RunOnStartup = false)] TimerInfo timer, ILogger log)
        {
            log.LogInformation("Request notification summary starting");

            //resourcesClient.GetAsync();

            //get requests with latest activity since last run which is read for company approval
            //get distinct approvers
            //for each approver
            //check email delay preferences for user. Use that delay.
            //if now - lastActivity > delay
            //include in notification summary
            //end if
            //send notification to user
            //next user

            //for requests ready for external approval
            //group by approver
            //check email delay preferences for user. Use that delay or min. 60 minutes.
            //get requests with last activity for the last <delay> minutes, in state
            //check when approvers were notified last for this request
            //if not notified, send notification
        }
    }
}
