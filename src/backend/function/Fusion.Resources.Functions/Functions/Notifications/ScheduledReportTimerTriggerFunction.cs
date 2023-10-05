using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Fusion.Resources.Functions.Functions.Notifications.API_Models;
using Fusion.Resources.Functions.Functions.Notifications.Models.DTOs;
using Fusion.Resources.Functions.Integration;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fusion.Resources.Functions.Functions.Notifications;

public class ScheduledReportTimerTriggerFunction
{
    private readonly HttpClient _lineOrgClient;
    private readonly ILogger<ScheduledReportTimerTriggerFunction> _logger;
    private readonly string _serviceBusConnectionString;
    private readonly string _queueName;

    public ScheduledReportTimerTriggerFunction(IHttpClientFactory httpClientFactory,
        ILogger<ScheduledReportTimerTriggerFunction> logger, IConfiguration configuration)
    {
        _lineOrgClient = httpClientFactory.CreateClient(HttpClientNames.Application.LineOrg);
        _logger = logger;
        _serviceBusConnectionString = configuration["AzureWebJobsServiceBus"];
        _queueName = configuration["scheduled_notification_report_queue"];
    }

    [FunctionName(ScheduledReportFunctionSettings.TimerTriggerFunctionName)]
    public async Task RunAsync(
        [TimerTrigger(ScheduledReportFunctionSettings.TimerTriggerFunctionSchedule, RunOnStartup = false)]
        TimerInfo scheduledReportTimer)
    {
        _logger.LogInformation(
            $"Function '{ScheduledReportFunctionSettings.TimerTriggerFunctionName}' " +
            $"started at: {DateTime.UtcNow}");
        try
        {
            var client = new ServiceBusClient(_serviceBusConnectionString);
            var sender = client.CreateSender(_queueName);

            await SendResourceOwnersToQueue(sender);

            _logger.LogInformation(
                $"Function '{ScheduledReportFunctionSettings.TimerTriggerFunctionName}' " +
                $"finished at: {DateTime.UtcNow}");
        }
        catch (Exception e)
        {
            _logger.LogError(
                $"Function '{ScheduledReportFunctionSettings.ContentBuilderFunctionName}' " +
                $"failed with exception: {e.Message}");
        }
    }

    private async Task SendResourceOwnersToQueue(ServiceBusSender sender)
    {
        try
        {
            // TODO: These resource-owners are handpicked to limit the report to scope of the project.
            var resourceOwners = await _lineOrgClient
                .GetAsJsonAsync<LineOrgPersons>(
                    $"/lineorg/persons?$filter=department in ('PDP', 'PRD', 'PMC', 'PCA') " +
                    $"and isResourceOwner eq 'true'");
            if (resourceOwners.Value == null || !resourceOwners.Value.Any())
                throw new Exception("No resource-owners found.");

            foreach (var value in resourceOwners.Value)
            {
                try
                {
                    if (string.IsNullOrEmpty(value.AzureUniqueId))
                        throw new Exception("Resource-owner azureUniqueId is empty.");

                    await SendDtoToQueue(sender, value.AzureUniqueId, NotificationRoleType.ResourceOwner);
                }
                catch (Exception e)
                {
                    _logger.LogError(
                        $"ServiceBus queue '{_queueName}' " +
                        $"item failed with exception when sending message: {e.Message}");
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(
                $"ServiceBus queue '{_queueName}' " +
                $"failed collecting resource-owners with exception: {e.Message}");
        }
    }

    private async Task SendDtoToQueue(ServiceBusSender sender, string azureUniqueId, NotificationRoleType role)
    {
        var dto = new ScheduledNotificationQueueDto
        {
            AzureUniqueId = azureUniqueId,
            Role = role
        };
        await sender.SendMessageAsync(new ServiceBusMessage(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dto))));
    }
}