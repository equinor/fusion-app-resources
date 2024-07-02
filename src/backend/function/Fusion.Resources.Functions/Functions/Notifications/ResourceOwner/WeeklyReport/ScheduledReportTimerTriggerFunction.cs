using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Fusion.Resources.Functions.ApiClients;
using Fusion.Resources.Functions.ApiClients.ApiModels;
using Fusion.Resources.Functions.Functions.Notifications.ResourceOwner.WeeklyReport.DTOs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fusion.Resources.Functions.Functions.Notifications.ResourceOwner.WeeklyReport;

public class ScheduledReportTimerTriggerFunction
{
    private readonly ILineOrgApiClient _lineOrgClient;
    private readonly ILogger<ScheduledReportTimerTriggerFunction> _logger;
    private readonly string _serviceBusConnectionString;
    private readonly string _queueName;

    // The function should start 02:10 and in order to be finished before 07:00 it spaces out the batch work over 4.5 hours
    private int _totalBatchTimeInMinutes = 270;

    public ScheduledReportTimerTriggerFunction(ILineOrgApiClient lineOrgApiClient,
        ILogger<ScheduledReportTimerTriggerFunction> logger, IConfiguration configuration)
    {
        _lineOrgClient = lineOrgApiClient;
        _logger = logger;
        _serviceBusConnectionString = configuration["AzureWebJobsServiceBus"];
        _queueName = configuration["scheduled_notification_report_queue"];

        // Handling reading 'total_batch_time_in_minutes' from configuration
        var totalBatchTimeInMinutesStr = configuration["total_batch_time_in_minutes"];

        if (!string.IsNullOrEmpty(totalBatchTimeInMinutesStr))
        {
            _totalBatchTimeInMinutes = int.Parse(totalBatchTimeInMinutesStr);
        }
        else
        {
            logger.LogWarning("Env variable 'scheduled_notification_report_queue' not found, using default '120'.");

            _totalBatchTimeInMinutes = 120;
        }
    }

    [FunctionName("scheduled-report-timer-trigger-function")]
    public async Task RunAsync(
        [TimerTrigger("0 10 0 * * MON", RunOnStartup = false)]
        TimerInfo scheduledReportTimer)
    {
        _logger.LogInformation(
            $"{nameof(ScheduledReportTimerTriggerFunction)} " +
            $"started at: {DateTime.UtcNow}");

        try
        {
            var client = new ServiceBusClient(_serviceBusConnectionString);
            var sender = client.CreateSender(_queueName);

            await SendResourceOwnersToQueue(sender, _totalBatchTimeInMinutes);

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

    private async Task SendResourceOwnersToQueue(ServiceBusSender sender, int totalBatchTimeInMinutes)
    {
        try
        {
            var departments = (await _lineOrgClient.GetOrgUnitDepartmentsAsync()).ToList();

            if (departments == null || !departments.Any())
                throw new Exception("No departments found.");

            var selectedDepartments = departments
                .Where(d => d.FullDepartment != null && d.FullDepartment.Contains("PRD")).Distinct().ToList();

            var resourceOwners = await GetLineOrgPersonsFromDepartmentsChunked(selectedDepartments);

            if (resourceOwners == null || !resourceOwners.Any())
                throw new Exception("No resource-owners found.");

            var resourceOwnersToSendNotifications = resourceOwners.DistinctBy(ro => ro.AzureUniqueId).ToList();

            var batchTimeInMinutes = totalBatchTimeInMinutes * 1f / resourceOwnersToSendNotifications.Count;

            _logger.LogInformation($"Batching time is calculated to {batchTimeInMinutes.ToString("F2")} minutes ({(60 * batchTimeInMinutes).ToString("F2")} sec)");

            var resourceOwnerMessageSent = 0;

            foreach (var resourceOwner in resourceOwnersToSendNotifications)
            {
                var timeDelayInMinutes = resourceOwnerMessageSent * batchTimeInMinutes;

                try
                {
                    if (string.IsNullOrEmpty(resourceOwner.AzureUniqueId))
                        throw new Exception("Resource-owner azureUniqueId is empty.");

                    await SendDtoToQueue(sender, new ScheduledNotificationQueueDto()
                    {
                        AzureUniqueId = resourceOwner.AzureUniqueId,
                        FullDepartment = resourceOwner.FullDepartment,
                        DepartmentSapId = resourceOwner.DepartmentSapId
                    }, timeDelayInMinutes);

                    resourceOwnerMessageSent++;
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

        _logger.LogInformation("Job completed");
    }

    private async Task<List<LineOrgPerson>> GetLineOrgPersonsFromDepartmentsChunked(
        List<LineOrgApiClient.OrgUnits> selectedDepartments)
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

    private async Task SendDtoToQueue(ServiceBusSender sender, ScheduledNotificationQueueDto dto, double delayInMinutes)
    {
        var serializedDto = JsonConvert.SerializeObject(dto);
        var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(serializedDto))
        {
            ScheduledEnqueueTime = DateTime.UtcNow.AddMinutes(delayInMinutes)
        };
        await sender.SendMessageAsync(message);
    }
}