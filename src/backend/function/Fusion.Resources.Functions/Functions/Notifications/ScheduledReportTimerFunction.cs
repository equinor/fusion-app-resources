using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Fusion.Resources.Functions.ApiClients;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Fusion.Resources.Functions.Functions.Notifications;

public class ScheduledReportTimerFunction
{
    private readonly IResourcesApiClient _resourcesClient;
    private readonly IPeopleApiClient _peopleClient;
    private readonly ILineOrgApiClient _lineOrgClient;
    private readonly ILogger<ScheduledReportTimerFunction> _logger;

    private const string QueueName = "queue-name";
    private const string ServiceBusConnectionString = "service-bus-connection-string";

    public ScheduledReportTimerFunction(IPeopleApiClient peopleClient, IResourcesApiClient resourcesClient,
        ILineOrgApiClient lineOrgClient, ILogger<ScheduledReportTimerFunction> logger)
    {
        _resourcesClient = resourcesClient;
        _lineOrgClient = lineOrgClient;
        _peopleClient = peopleClient;
        _logger = logger;
    }

    [FunctionName("scheduled-report-timer-function")]
    public async Task RunAsync([TimerTrigger("0 0 6 * * 0")] TimerInfo myTimer, ILogger log)
    {
        _logger.LogInformation($"function executed at: {DateTime.UtcNow}");
        var client = new ServiceBusClient(ServiceBusConnectionString);
        var sender = client.CreateSender(QueueName);

        // Todo: Collect positionIds from database
        foreach (var positionId in new List<Guid>{ Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() })
        {
            await SendPositionIdToQue(sender, positionId);
        }
    }

    private async Task SendPositionIdToQue(ServiceBusSender sender, Guid positionId)
    {
        if (positionId == Guid.Empty)
        {
            _logger.LogError($"Service Bus Queue ({QueueName}): Error sending message: positionId is empty");
            return;
        }
        await SendMessageToQue(sender, positionId.ToString());
    }

    private async Task SendMessageToQue(ServiceBusSender sender, string message)
    {
        try
        {
            // Create a message
            var serviceBusMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(message));
            // Send the message to the queue
            await sender.SendMessageAsync(serviceBusMessage);

            _logger.LogInformation($"Service Bus Queue ({QueueName}): Message sent to que: {message}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Service Bus Queue ({QueueName}): Error sending message: {ex.Message}");
        }
    }
}