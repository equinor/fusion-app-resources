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
using static Fusion.Resources.Functions.ApiClients.LineOrgApiClient;

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
        [TimerTrigger("0 10 0 * * MON", RunOnStartup = true)]
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
                .Where(x => x.management?.Persons.Length > 0);            // Exclude departments with no receivers

            // Group OrgUnits by FullDepartment and join the Person arrays together
            var groupedDepartments = departments
                .GroupBy(orgUnit => orgUnit.FullDepartment)
                .Select(group =>
                {
                    // Combine the Person arrays of all OrgUnits in the group into a single array
                    var allPersons = group.SelectMany(orgUnit => orgUnit.management.Persons).ToArray();

                    // Create a new OrgUnits object with the FullDepartment, SapId, and combined Person array
                    return new OrgUnits
                    {
                        FullDepartment = group.Key,
                        SapId = group.First().SapId,
                        management = new Management { Persons = allPersons }
                    };
                })
                .ToList();

            // Calculate batch time based of total number of departments and the allowed run time
            var batchTimeInMinutes = CalculateBatchTime(totalBatchTimeInMinutes, groupedDepartments.Count);

            // Send the queue for processing

            var resourceOwnerMessageSent = 0;

            foreach (var dep in groupedDepartments)
            {
                var timeDelayInMinutes = resourceOwnerMessageSent * batchTimeInMinutes;

                try
                {
                    await SendDtoToQueue(sender, new ScheduledNotificationQueueDto
                    {
                        AzureUniqueId = dep.management.Persons.Select(x => x.AzureUniqueId),
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