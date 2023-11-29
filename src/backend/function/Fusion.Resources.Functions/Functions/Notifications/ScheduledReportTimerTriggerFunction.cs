using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Fusion.Resources.Functions.ApiClients;
using Fusion.Resources.Functions.ApiClients.ApiModels;
using Fusion.Resources.Functions.Functions.Notifications.Models.DTOs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fusion.Resources.Functions.Functions.Notifications;

public class ScheduledReportTimerTriggerFunction
{
    private readonly ILineOrgApiClient _lineOrgClient;
    private readonly ILogger<ScheduledReportTimerTriggerFunction> _logger;
    private readonly string _serviceBusConnectionString;
    private readonly string _queueName;

    public ScheduledReportTimerTriggerFunction(ILineOrgApiClient lineOrgApiClient,
        ILogger<ScheduledReportTimerTriggerFunction> logger, IConfiguration configuration)
    {
        _lineOrgClient = lineOrgApiClient;
        _logger = logger;
        _serviceBusConnectionString = configuration["AzureWebJobsServiceBus"];
        _queueName = configuration["scheduled_notification_report_queue"];
    }

    [FunctionName("scheduled-report-timer-trigger-function")]
    public async Task RunAsync(
        [TimerTrigger("0 0 8 * * MON", RunOnStartup = false)]
        TimerInfo scheduledReportTimer)
    {
        _logger.LogInformation(
            $"{nameof(ScheduledReportTimerTriggerFunction)} " +
            $"started at: {DateTime.UtcNow}");
        try
        {
            var client = new ServiceBusClient(_serviceBusConnectionString);
            var sender = client.CreateSender(_queueName);

            await SendResourceOwnersToQueue(sender);

            _logger.LogInformation(
                $"{nameof(ScheduledReportTimerTriggerFunction)} " +
                $"finished at: {DateTime.UtcNow}");
        }
        catch (Exception e)
        {
            _logger.LogError(
                $"{nameof(ScheduledReportTimerTriggerFunction)} " +
                $"failed with exception: {e.Message}");
        }
    }

    private async Task SendResourceOwnersToQueue(ServiceBusSender sender)
    {
        try
        {
            var departments = await _lineOrgClient.GetOrgUnitDepartmentsAsync();
            if (departments == null || !departments.Any())
                throw new Exception("No departments found.");

            // TODO: These resource-owners are handpicked to limit the scope of the project.
            var selectedDepartments = departments.Where(d => d.Contains("PRD")).Distinct().ToList();
            var resourceOwners = await GetLineOrgPersonsFromDepartmetnsChunked(selectedDepartments);

            if (resourceOwners == null || !resourceOwners.Any())
                throw new Exception("No resource-owners found.");

            foreach (var resourceOwner in resourceOwners.DistinctBy(ro => ro.AzureUniqueId))
            {
                try
                {
                    if (string.IsNullOrEmpty(resourceOwner.AzureUniqueId))
                        throw new Exception("Resource-owner azureUniqueId is empty.");

                    await SendDtoToQueue(sender, new ScheduledNotificationQueueDto()
                    {
                        AzureUniqueId = resourceOwner.AzureUniqueId,
                        FullDepartment = resourceOwner.FullDepartment,
                        Role = NotificationRoleType.ResourceOwner
                    });
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

    private async Task<List<LineOrgPerson>> GetLineOrgPersonsFromDepartmetnsChunked(List<string> selectedDepartments)
    {
        var resourceOwners = new List<LineOrgPerson>();
        const int chuckSize = 10;
        for (var i = 0; i < selectedDepartments.Count; i += chuckSize)
        {
            var chunk = selectedDepartments.Skip(i).Take(chuckSize).ToList();
            var chunkedResourceOwners =
                await _lineOrgClient.GetResourceOwnersFromFullDepartment(chunk);
            resourceOwners.AddRange(chunkedResourceOwners);
        }

        return resourceOwners;
    }

    private async Task SendDtoToQueue(ServiceBusSender sender, ScheduledNotificationQueueDto dto)
    {
        var serializedDto = JsonConvert.SerializeObject(dto);
        await sender.SendMessageAsync(new ServiceBusMessage(Encoding.UTF8.GetBytes(serializedDto)));
    }
}