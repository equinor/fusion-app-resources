using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Fusion.Resources.Functions.ApiClients;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Fusion.Resources.Functions.Functions.Notifications;

public class ScheduledReportTimerTriggerFunction
{
    private readonly IResourcesApiClient _resourcesClient;
    private readonly IPeopleApiClient _peopleClient;
    private readonly ILineOrgApiClient _lineOrgClient;
    private readonly ILogger<ScheduledReportTimerTriggerFunction> _logger;

    public ScheduledReportTimerTriggerFunction(IPeopleApiClient peopleClient, IResourcesApiClient resourcesClient,
        ILineOrgApiClient lineOrgClient, ILogger<ScheduledReportTimerTriggerFunction> logger)
    {
        _resourcesClient = resourcesClient;
        _lineOrgClient = lineOrgClient;
        _peopleClient = peopleClient;
        _logger = logger;
    }

    [FunctionName(ScheduledReportFunctionSettings.TimerTriggerFunctionName)]
    public async Task RunAsync(
        [TimerTrigger(ScheduledReportFunctionSettings.TimerTriggerFunctionSchedule)]
        TimerInfo scheduledReportTimer)
    {
        _logger.LogInformation(
            $"Function '{ScheduledReportFunctionSettings.TimerTriggerFunctionName}' started at: {DateTime.UtcNow}");
        
        var client = new ServiceBusClient(ScheduledReportServiceBusSettings.ServiceBusConnectionString);
        var sender = client.CreateSender(ScheduledReportServiceBusSettings.QueueName);

        // Todo: Collect positionIds from API
        foreach (var positionId in new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() })
        {
            await SendPositionIdToQue(sender, positionId);
        }
        _logger.LogInformation(
            $"Function '{ScheduledReportFunctionSettings.TimerTriggerFunctionName}' finished at: {DateTime.UtcNow}");
    }

    private async Task SendPositionIdToQue(ServiceBusSender sender, Guid positionId)
    {
        if (positionId == Guid.Empty)
        {
            _logger.LogError(
                $"ServiceBus queue ({ScheduledReportServiceBusSettings.QueueName}), error sending message: positionId is empty");
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

            _logger.LogInformation(
                $"ServiceBus queue {ScheduledReportServiceBusSettings.QueueName}, message sent to que: {message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                $"ServiceBus queue {ScheduledReportServiceBusSettings.QueueName}, error sending message: {ex.Message}");
        }
    }
}