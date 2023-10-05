using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AdaptiveCards;
using Azure.Messaging.ServiceBus;
using Fusion.Resources.Functions.ApiClients;
using Fusion.Resources.Functions.Functions.Notifications.Models;
using Fusion.Resources.Functions.Functions.Notifications.Models.AdaptiveCard_Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fusion.Resources.Functions.Functions.Notifications;

public class ScheduledReportContentBuilderFunction
{
    private readonly ILogger<ScheduledReportContentBuilderFunction> _logger;
    private readonly string _queueName;
    private readonly HttpClient _notificationsClient;

    public ScheduledReportContentBuilderFunction(ILogger<ScheduledReportContentBuilderFunction> logger,
        IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _queueName = configuration["scheduled_notification_report_queue"];
        _notificationsClient = httpClientFactory.CreateClient(HttpClientNames.Application.Notifications);
    }

    [FunctionName(ScheduledReportFunctionSettings.ContentBuilderFunctionName)]
    public async Task RunAsync(
        [ServiceBusTrigger("%scheduled_notification_report_queue%", Connection = "AzureWebJobsServiceBus")]
        ServiceBusReceivedMessage message, ServiceBusMessageActions messageReceiver)
    {
        _logger.LogInformation(
            $"Function '{ScheduledReportFunctionSettings.ContentBuilderFunctionName}' started with message: {message.Body}");
        try
        {
            var body = Encoding.UTF8.GetString(message.Body);
            var dto = JsonConvert.DeserializeObject<ScheduledNotificationQueueDto>(body);
            if (!Guid.TryParse(dto.AzureUniqueId, out var azureUniqueId))
                throw new Exception("AzureUniqueId not valid.");

            switch (dto.Role)
            {
                case NotificationRoleType.ResourceOwner:
                    await BuildContentForResourceOwner(azureUniqueId);
                    break;
                case NotificationRoleType.TaskOwner:
                    await BuildContentForTaskOwner(azureUniqueId);
                    break;
                default:
                    throw new Exception("Role not valid.");
            }

            // TODO: The message should be completed after the email has been sent.
            await messageReceiver.CompleteMessageAsync(message);

            _logger.LogInformation(
                $"Function '{ScheduledReportFunctionSettings.ContentBuilderFunctionName}' finished with message: {message.Body}");
        }
        catch (Exception e)
        {
            _logger.LogError(
                $"Function '{ScheduledReportFunctionSettings.ContentBuilderFunctionName}' failed with exception: {e.Message}");
        }
    }

    private async Task BuildContentForTaskOwner(Guid azureUniqueId)
    {
        throw new NotImplementedException();
    }

    private async Task BuildContentForResourceOwner(Guid azureUniqueId)
    {
        var card = ResourceOwnerAdaptiveCardBuilder(new ResourceOwnerAdaptiveCardData());

        throw new NotImplementedException();
    }

    private static AdaptiveCard ResourceOwnerAdaptiveCardBuilder(ResourceOwnerAdaptiveCardData cardData)
    {
        var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 5));

        card.Body.Add(new AdaptiveTextBlock
        {
            Text = "Department Statistics",
            Size = AdaptiveTextSize.Large,
            Weight = AdaptiveTextWeight.Bolder
        });

        card.Body.Add(new AdaptiveFactSet
        {
            Facts = new List<AdaptiveFact>
            {
                new("Total requests", cardData.TotalNumberOfRequests.ToString()),
                new("Requests starting after 3 months", cardData.NumberOfOlderRequests.ToString()),
                new("Requests staring within 3 months (no nomination)",
                    cardData.NumberOfNewRequestsWithNoNomination.ToString()),
                new("Open requests", cardData.NumberOfOpenRequests.ToString()),
                new("Personnel positions ending withing 3 months(No Future Allocation)",
                    cardData.PersonnelPositionsEndingWithNoFutureAllocation.Aggregate((a, b) => "a, b")),
                new("Percent allocation of total capacity", cardData.PercentAllocationOfTotalCapacity.ToString()),
                new("Personnel allocated more than 100%",
                    cardData.NumberOfPersonnelAllocatedMoreThan100Percent.ToString()),
                new("EXT contracts ending in 3 months", cardData.NumberOfExtContractsEnding.ToString())
            }
        });
        return card;
    }
}