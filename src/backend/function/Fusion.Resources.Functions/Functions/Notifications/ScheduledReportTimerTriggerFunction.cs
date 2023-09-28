using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Fusion.Resources.Api.Controllers;
using Fusion.Resources.Functions.ApiClients;
using Fusion.Resources.Functions.Functions.Notifications.API_Models;
using Fusion.Resources.Functions.Integration;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Fusion.Resources.Functions.Functions.Notifications;

public class ScheduledReportTimerTriggerFunction
{
    private readonly HttpClient _resourcesClient;
    private readonly HttpClient _lineOrgClient;
    private readonly ILogger<ScheduledReportTimerTriggerFunction> _logger;

    public ScheduledReportTimerTriggerFunction(IHttpClientFactory httpClientFactory,
        ILogger<ScheduledReportTimerTriggerFunction> logger)
    {
        _resourcesClient = httpClientFactory.CreateClient(HttpClientNames.Application.Resources);
        _lineOrgClient = httpClientFactory.CreateClient(HttpClientNames.Application.LineOrg);
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

        // TODO: These resource-owners are handpicked to limit the report to scope of the project.
        var resourceOwners = await _lineOrgClient
            .GetAsJsonAsync<LineOrgPersons>($"/lineorg/persons?$filter=department in ('PDP', 'PRD', 'PMC', 'PCA')&$isResourceOwner eq true");
        if (resourceOwners.Value == null || !resourceOwners.Value.Any())
        {
            _logger.LogError(
                $"ServiceBus queue '{ScheduledReportServiceBusSettings.QueueName}', error sending message: resourceOwners is empty");
            return;
        }

        foreach (var azureId in resourceOwners.Value.Select(r => r.AzureUniqueId))
        {
            await SendPositionIdToQue(sender, azureId);
        }

        _logger.LogInformation(
            $"Function '{ScheduledReportFunctionSettings.TimerTriggerFunctionName}' finished at: {DateTime.UtcNow}");
    }

    private async Task SendPositionIdToQue(ServiceBusSender sender, string azureId)
    {
        if (string.IsNullOrEmpty(azureId))
        {
            _logger.LogError(
                $"ServiceBus queue '{ScheduledReportServiceBusSettings.QueueName}', error sending message: azureId is empty");
            return;
        }
        await SendMessageToQue(sender, azureId);
    }

    private async Task SendMessageToQue(ServiceBusSender sender, string message)
    {
        try
        {
            await sender.SendMessageAsync(new ServiceBusMessage(Encoding.UTF8.GetBytes(message)));

            _logger.LogInformation(
                $"ServiceBus queue '{ScheduledReportServiceBusSettings.QueueName}', message sent to que: {message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                $"ServiceBus queue '{ScheduledReportServiceBusSettings.QueueName}', error sending message: {ex.Message}");
        }
    }
}