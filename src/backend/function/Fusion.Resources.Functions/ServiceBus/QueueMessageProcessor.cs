using System;
using System.Text;
using System.Threading.Tasks;
using AdaptiveCards;
using Azure.Messaging.ServiceBus;
using Fusion.Resources.Functions.ApiClients;
using Fusion.Resources.Functions.ApiClients.ApiModels;
using Fusion.Resources.Functions.Functions.Notifications;
using Fusion.Resources.Integration.Models.Queue;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fusion.Resources.Functions;

public class QueueMessageProcessor
{
    private readonly ILogger log;
    private readonly ServiceBusMessageActions receiver;
    private readonly IAsyncCollector<ServiceBusMessage> sender;
    private readonly IConfiguration configuration;
    private readonly IResourcesApiClient resourceClient;
    private readonly INotificationApiClient notificationsClient;
    private readonly IPeopleApiClient peopleClient;

    private const int MaxRetryCount = 5;


    public QueueMessageProcessor(
        ILogger log,
        ServiceBusMessageActions receiver,
        IAsyncCollector<ServiceBusMessage> sender,
        IConfiguration configuration,
        IResourcesApiClient resourceClient,
        INotificationApiClient notificationsClient, IPeopleApiClient peopleClient)
    {
        this.log = log;
        this.receiver = receiver;
        this.sender = sender;
        this.configuration = configuration;
        this.resourceClient = resourceClient;
        this.notificationsClient = notificationsClient;
        this.peopleClient = peopleClient;
    }


    public async Task ProcessWithRetriesAsync(ServiceBusReceivedMessage message, Func<string, ILogger, Task> action)
    {
        try
        {
            var messageBody = Encoding.UTF8.GetString(message.Body);
            log.LogInformation($"C# Queue trigger function processed: {messageBody}");

            log.LogInformation(
                $"C# ServiceBus queue trigger function processed message sequence #{message.SequenceNumber}");
            await action(messageBody, log);

            await receiver.CompleteMessageAsync(message);

            // Send notification
            await ProvisioningNotification(message, "Request was successfully provisioned");
        }
        catch (Exception ex)
        {
            log.LogError(ex, ex.Message);
            log.LogInformation("Calculating exponential retry");


            int retryCount = 0;
            long originalSequence = message.SequenceNumber;

            // If the message doesn't have a retry-count, set as 0
            if (message.ApplicationProperties.ContainsKey("retry-count"))
            {
                retryCount = (int)message.ApplicationProperties["retry-count"];
                originalSequence = (long)message.ApplicationProperties["original-SequenceNumber"];
            }

            // If there are more retries available
            if (retryCount < MaxRetryCount)
            {
                var retryMessage = new ServiceBusMessage(message);
                retryCount++;
                var interval = 30 * retryCount;
                var scheduledTime = DateTimeOffset.Now.AddSeconds(interval);

                retryMessage.ApplicationProperties["retry-count"] = retryCount;
                retryMessage.ApplicationProperties["original-SequenceNumber"] = originalSequence;
                retryMessage.ScheduledEnqueueTime = scheduledTime;
                await sender.AddAsync(retryMessage);
                await receiver.CompleteMessageAsync(message);

                log.LogInformation(
                    $"Scheduling message retry {retryCount} to wait {interval} seconds and arrive at {scheduledTime.UtcDateTime}");
                throw;
            }

            // If there are no more retries, deadletter the message (note the host.json config that enables this)
            log.LogCritical(
                $"Exhausted all retries for message sequence # {message.ApplicationProperties["original-SequenceNumber"]}");
            await receiver.DeadLetterMessageAsync(message, "Exhausted all retries");

            // Send notification
            await ProvisioningNotification(message, "Request failed in provisioning");
            throw;
        }
    }

    private async Task ProvisioningNotification(ServiceBusReceivedMessage message, string title)
    {
        var body = Encoding.UTF8.GetString(message.Body);
        var provisionPosition = JsonConvert.DeserializeObject<ProvisionPositionMessageV1>(body);
        if (provisionPosition is null)
            return;

        var portalUri = PortalUri();
        var request = await resourceClient
            .GetRequest(provisionPosition.ProjectOrgId, provisionPosition.RequestId);

        // Task owner
        if (request.CreatedBy != null)
        {
            await SendNotification(
                ProvisioningCard(title, request,
                    "Go to Project organisation", $"{portalUri}apps/pro-org/{provisionPosition.ProjectOrgId}/chart"),
                request.CreatedBy.AzureUniquePersonId, title);
        }

        // Resource owner
        if (request.UpdatedBy != null)
        {
            var resourceOwner = await peopleClient.GetPerson(request.UpdatedBy.AzureUniquePersonId);
            await SendNotification(
                ProvisioningCard(title, request,
                    "Go to Personnel allocation", $"{portalUri}apps/personnel-allocation/{resourceOwner.SapId}"),
                request.UpdatedBy.AzureUniquePersonId, title);
        }
    }

    private static AdaptiveCard ProvisioningCard(
        string title,
        IResourcesApiClient.ResourceAllocationRequest request,
        string buttonName, string buttonUri)
    {
        var taskOwnerCard = new AdaptiveCardBuilder()
            .AddHeading($"**{title}**")
            .AddColumnSet(new AdaptiveCardBuilder.AdaptiveCardColumn($"{request.Number}", "Request number"))
            .AddColumnSet(new AdaptiveCardBuilder.AdaptiveCardColumn($"{request.AssignedDepartment}", "Department"))
            .AddColumnSet(new AdaptiveCardBuilder.AdaptiveCardColumn($"{request.Id}", "Request id"))
            .AddNewLine()
            .AddActionButton(buttonName, buttonUri)
            .Build();
        return taskOwnerCard;
    }

    private async Task SendNotification(
        AdaptiveCard card,
        Guid? sendToAzureId,
        string title)
    {
        if (sendToAzureId is null)
            return;

        await notificationsClient.SendNotification(
            new SendNotificationsRequest()
            {
                Title = $"{title}",
                EmailPriority = 1,
                Card = card,
                Description = $"{title}",
            },
            sendToAzureId.Value);
    }

    private string PortalUri()
    {
        var portalUri = configuration["Endpoints_portal"] ?? "https://fusion.equinor.com/";
        if (!portalUri.EndsWith("/"))
            portalUri += "/";
        return portalUri;
    }
}