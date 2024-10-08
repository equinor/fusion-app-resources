﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Fusion.Resources.Functions.Common.ApiClients;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fusion.Summary.Functions.Functions;

public class DepartmentResourceOwnerSync
{
    private readonly ILineOrgApiClient lineOrgApiClient;
    private readonly ISummaryApiClient summaryApiClient;
    private readonly IResourcesApiClient resourcesApiClient;
    private readonly ILogger<DepartmentResourceOwnerSync> logger;

    private string _serviceBusConnectionString;
    private string _weeklySummaryQueueName;
    private string[] _departmentFilter;
    private TimeSpan _totalBatchTime;

    public DepartmentResourceOwnerSync(
        ILineOrgApiClient lineOrgApiClient,
        ISummaryApiClient summaryApiClient,
        IConfiguration configuration,
        IResourcesApiClient resourcesApiClient,
        ILogger<DepartmentResourceOwnerSync> logger)
    {
        this.lineOrgApiClient = lineOrgApiClient;
        this.summaryApiClient = summaryApiClient;
        this.resourcesApiClient = resourcesApiClient;
        this.logger = logger;

        _serviceBusConnectionString = configuration["AzureWebJobsServiceBus"];
        _weeklySummaryQueueName = configuration["department_summary_weekly_queue"];
        _departmentFilter = configuration["departmentFilter"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? ["PRD"];

        var totalBatchTimeInMinutesStr = configuration["total_batch_time_in_minutes"];

        if (!string.IsNullOrWhiteSpace(totalBatchTimeInMinutesStr))
        {
            _totalBatchTime = TimeSpan.FromMinutes(double.Parse(totalBatchTimeInMinutesStr));
            logger.LogInformation("Batching messages over {BatchTime}", _totalBatchTime);
        }
        else
        {
            _totalBatchTime = TimeSpan.FromHours(4.5);

            logger.LogInformation("Configuration variable 'total_batch_time_in_minutes' not found, batching messages over {BatchTime}", _totalBatchTime);
        }
    }

    /// <summary>
    /// Function does two things:
    /// - Fetches all departments and updates the database
    /// - Sends the department info to the weekly summary queue for the workers to pick up
    /// </summary>
    /// <param name="timerInfo">The running date & time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [FunctionName("weekly-department-recipients-sync")]
    public async Task RunAsync(
        [TimerTrigger("0 5 0 * * MON", RunOnStartup = false)]
        TimerInfo timerInfo, CancellationToken cancellationToken
    )
    {
        var client = new ServiceBusClient(_serviceBusConnectionString);
        var sender = client.CreateSender(_weeklySummaryQueueName);

        logger.LogInformation("weekly-department-recipients-sync fetching departments with department filter {DepartmentFilter}", JsonConvert.SerializeObject(_departmentFilter, Formatting.Indented));

        // Fetch all departments
        var departments = (await lineOrgApiClient.GetOrgUnitDepartmentsAsync())
            .DistinctBy(d => d.SapId)
            .Where(d => d.FullDepartment != null && d.SapId != null);

        if (_departmentFilter.Length != 0)
            departments = departments.Where(d => _departmentFilter.Any(df => d.FullDepartment!.Contains(df)));

        logger.LogInformation("Found departments {Departments}", JsonConvert.SerializeObject(departments, Formatting.Indented));

        var apiDepartments = new List<ApiResourceOwnerDepartment>();

        foreach (var orgUnit in departments)
        {
            var resourceOwners = orgUnit.Management.Persons
                .Select(p => Guid.Parse(p.AzureUniqueId))
                .Distinct()
                .ToArray();

            var delegatedResponsibles = (await resourcesApiClient
                    .GetDelegatedResponsibleForDepartment(orgUnit.SapId!))
                .Select(d => Guid.Parse(d.DelegatedResponsible.AzureUniquePersonId))
                .Distinct()
                .ToArray();

            var recipients = resourceOwners.Concat(delegatedResponsibles).ToArray();
            if (recipients.Length == 0)
            {
                logger.LogInformation("Skipping department {Department} as it has no resource owners or delegated responsibles", orgUnit.FullDepartment);
                continue;
            }

            apiDepartments.Add(new ApiResourceOwnerDepartment()
            {
                DepartmentSapId = orgUnit.SapId!,
                FullDepartmentName = orgUnit.FullDepartment!,
                ResourceOwnersAzureUniqueId = resourceOwners,
                DelegateResourceOwnersAzureUniqueId = delegatedResponsibles
            });
        }

        var enqueueTimeForDepartmentMapping = CalculateDepartmentEnqueueTime(apiDepartments);

        logger.LogInformation("Syncing departments {Departments}", JsonConvert.SerializeObject(enqueueTimeForDepartmentMapping, Formatting.Indented));


        foreach (var department in apiDepartments)
        {
            try
            {
                //TODO: Do one batch update instead of individual updates
                // Update the database
                await summaryApiClient.PutDepartmentAsync(department, cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogCritical(e, "Failed to PUT department {Department}", JsonConvert.SerializeObject(department, Formatting.Indented));
                continue;
            }

            try
            {
                // Send queue message
                await SendDepartmentToQueue(sender, department, enqueueTimeForDepartmentMapping[department]);
            }
            catch (Exception e)
            {
                logger.LogCritical(e, "Failed to send department to queue {Department}", JsonConvert.SerializeObject(department, Formatting.Indented));
            }
        }

        logger.LogInformation("weekly-department-recipients-sync completed");
    }


    private async Task SendDepartmentToQueue(ServiceBusSender sender, ApiResourceOwnerDepartment department, DateTimeOffset enqueueTime)
    {
        var serializedDto = JsonConvert.SerializeObject(department);

        var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(serializedDto))
        {
            ScheduledEnqueueTime = enqueueTime
        };

        await sender.SendMessageAsync(message);
    }

    /// <summary>
    ///     Calculate the enqueue time for each department based on the total batch time and amount of departments. This should spread
    ///     the work over the total batch time.
    /// </summary>
    private Dictionary<ApiResourceOwnerDepartment, DateTimeOffset> CalculateDepartmentEnqueueTime(List<ApiResourceOwnerDepartment> apiDepartments)
    {
        var currentTime = DateTimeOffset.UtcNow;
        var minutesPerReportSlice = _totalBatchTime.TotalMinutes / apiDepartments.Count;

        logger.LogInformation("Minutes allocated for each worker: {MinutesPerReportSlice}", minutesPerReportSlice);

        var departmentDelayMapping = new Dictionary<ApiResourceOwnerDepartment, DateTimeOffset>();
        foreach (var department in apiDepartments)
        {
            // First department has no delay
            if (departmentDelayMapping.Count == 0)
            {
                departmentDelayMapping.Add(department, currentTime);
                continue;
            }

            var enqueueTime = departmentDelayMapping.Last().Value.AddMinutes(minutesPerReportSlice);
            departmentDelayMapping.Add(department, enqueueTime);
        }

        return departmentDelayMapping;
    }
}