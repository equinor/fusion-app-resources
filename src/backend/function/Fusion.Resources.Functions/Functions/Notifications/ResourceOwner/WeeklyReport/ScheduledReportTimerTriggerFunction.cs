using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Fusion.Resources.Functions.Common.ApiClients;
using Fusion.Resources.Functions.Common.ApiClients.ApiModels;
using Fusion.Resources.Functions.Functions.Notifications.ResourceOwner.WeeklyReport.DTOs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static Fusion.Resources.Functions.Common.ApiClients.LineOrgApiClient;

namespace Fusion.Resources.Functions.Functions.Notifications.ResourceOwner.WeeklyReport;

public class ScheduledReportTimerTriggerFunction
{
    private readonly ILineOrgApiClient _lineOrgClient;
    private readonly IResourcesApiClient _resourcesApiClient;
    private readonly ILogger<ScheduledReportTimerTriggerFunction> _logger;
    private readonly string _serviceBusConnectionString;
    private readonly string _queueName;

    // The function should start 02:10 and in order to be finished before 07:00 it spaces out the batch work over 4.5 hours
    private int _totalBatchTimeInMinutes = 270;

    public ScheduledReportTimerTriggerFunction(
        ILineOrgApiClient lineOrgApiClient,
        IResourcesApiClient resourcesApiClient,
        ILogger<ScheduledReportTimerTriggerFunction> logger,
        IConfiguration configuration)
    {
        _lineOrgClient = lineOrgApiClient;
        _resourcesApiClient = resourcesApiClient;
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
            // Query departments from LineOrg
            var departments = (await _lineOrgClient.GetOrgUnitDepartmentsAsync())
                .Where(d => d.FullDepartment != null)                     // Exclude departments with blank department name
                .Where(d => d.Level >= 4)                                 // Only include departments of level 4 and up
                .Where(x => x.Management?.Persons.Length > 0)             // Exclude departments with no receivers
                .ToList();

            // Calculate batch time based of total number of departments and the allowed run time
            var batchTimeInMinutes = CalculateBatchTime(totalBatchTimeInMinutes, departments.Count);

            var totalNumberOfDepartments = departments.Count;
            int totalNumberOfRecipients = departments.Sum(orgUnit => orgUnit.Management.Persons.Length);

            _logger.LogInformation($"With {totalNumberOfDepartments} departments it's going to send notification to {totalNumberOfRecipients} recipients");

            // Send the queue for processing

            var resourceOwnerMessageSent = 0;

            foreach (var dep in departments)
            {
                var notificationRecipients = dep.Management.Persons.Select(x => x.AzureUniqueId).ToList();

                // Get delegates for department, if any
                var delegatesResult = await _resourcesApiClient.GetDelegatedResponsibleForDepartment(dep.FullDepartment);

                // Add the user id to the list
                notificationRecipients.AddRange(delegatesResult.Select(x => x.DelegatedResponsible.AzureUniquePersonId));

                // Clean up duplicates in the list
                notificationRecipients = notificationRecipients.Distinct().ToList();

                var timeDelayInMinutes = resourceOwnerMessageSent++ * batchTimeInMinutes;

                try
                {
                    await SendDtoToQueue(sender, new ScheduledNotificationQueueDto
                    {
                        AzureUniqueId = notificationRecipients,
                        FullDepartment = dep.FullDepartment,
                        DepartmentSapId = dep.SapId
                    }, timeDelayInMinutes);
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

    private float CalculateBatchTime(int totalBatchTimeInMinutes, int departmentCount)
    {
        var batchTimeInMinutes = totalBatchTimeInMinutes * 1f / departmentCount;

        _logger.LogInformation($"Batching time is calculated to {batchTimeInMinutes.ToString("F2")} minutes ({(60 * batchTimeInMinutes).ToString("F2")} sec)");

        return batchTimeInMinutes;
    }
}