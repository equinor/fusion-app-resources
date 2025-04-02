using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdaptiveCards;
using Fusion.Resources.Functions.Common.ApiClients;
using Fusion.Resources.Functions.Common.ApiClients.ApiModels;
using Fusion.Summary.Functions.CardBuilder;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static Fusion.Summary.Functions.CardBuilder.AdaptiveCardBuilder;

namespace Fusion.Summary.Functions.Functions;

public class WeeklyDepartmentSummarySender
{
    private readonly ISummaryApiClient summaryApiClient;
    private readonly INotificationApiClient notificationApiClient;
    private readonly ILogger<WeeklyDepartmentSummarySender> logger;
    private readonly IConfiguration configuration;

    private int _maxDegreeOfParallelism;
    private readonly string[] _departmentFilter;
    private bool _sendingNotificationEnabled = true; // Default to true so that we don't accidentally disable sending notifications

    public WeeklyDepartmentSummarySender(ISummaryApiClient summaryApiClient, INotificationApiClient notificationApiClient,
        ILogger<WeeklyDepartmentSummarySender> logger, IConfiguration configuration)
    {
        this.summaryApiClient = summaryApiClient;
        this.notificationApiClient = notificationApiClient;
        this.logger = logger;
        this.configuration = configuration;

        _maxDegreeOfParallelism = int.TryParse(configuration["weekly-department-summary-sender-parallelism"], out var result) ? result : 2;
        _departmentFilter = configuration["departmentFilter"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? ["PRD"];

        // Need to explicitly add the configuration key to the app settings to disable sending of notifications
        if (int.TryParse(configuration["isSendingNotificationEnabled"], out var enabled))
            _sendingNotificationEnabled = enabled == 1;
        else if (bool.TryParse(configuration["isSendingNotificationEnabled"], out var enabledBool))
            _sendingNotificationEnabled = enabledBool;
    }

    /// <summary>
    ///     This function retrieves the latest weekly summary report for each department and sends a notification to the
    ///     resource owners and delegate resource owners.
    ///     <para>
    ///         Set the configuration key <c>departmentFilter</c> to filter the departments to send notifications to. Is set to
    ///         ["PRD"] by default.
    ///     </para>
    ///     <para>
    ///         Set the configuration key <c>isSendingNotificationEnabled</c> to true/false or 1/0 to enable/disable sending of notifications. Is
    ///         true by default. If disabled the function will log a message and skip sending notifications.
    ///     </para>
    ///     <para>
    ///         Set the configuration key <c>weekly-department-summary-sender-parallelism</c> to control the number of
    ///         notifications to send in parallel.
    ///         Be mindful that the notification API might struggle with too many parallel requests. Default is 2.
    ///     </para>
    /// </summary>
    [FunctionName("weekly-department-summary-sender")]
    public async Task RunAsync([TimerTrigger("0 0 5 * * MON", RunOnStartup = false)] TimerInfo timerInfo)
    {
        logger.LogInformation("weekly-department-summary-sender started with department filter {DepartmentFilter}", JsonConvert.SerializeObject(_departmentFilter, Formatting.Indented));

        if (!_sendingNotificationEnabled)
            logger.LogInformation("Sending of notifications is disabled");


        // TODO: Use OData query to filter departments
        var departments = await summaryApiClient.GetDepartmentsAsync();

        if (_departmentFilter.Length != 0)
            departments = departments?.Where(d => _departmentFilter.Any(df => d.FullDepartmentName!.Contains(df))).ToArray();

        if (departments is null || departments.Count == 0)
        {
            logger.LogCritical("No departments found. Exiting");
            return;
        }

        var options = new ParallelOptions()
        {
            MaxDegreeOfParallelism = _maxDegreeOfParallelism
        };

        logger.LogInformation("Running sender with {MaxDegreeOfParallelism} parallel tasks", _maxDegreeOfParallelism);

        // Use Parallel.ForEachAsync to easily limit the number of parallel requests
        await Parallel.ForEachAsync(departments, options, async (department, _) => await CreateAndSendNotificationsAsync(department));

        logger.LogInformation("weekly-department-summary-sender completed");
    }

    private async Task CreateAndSendNotificationsAsync(ApiResourceOwnerDepartment department)
    {
        ApiWeeklySummaryReport summaryReport;

        try
        {
            summaryReport = await summaryApiClient.GetLatestWeeklyReportAsync(department.DepartmentSapId);

            if (summaryReport is null)
            {
                // There can be valid cases where there is no summary report for a department. E.g. if the department has no personnel
                logger.LogWarning(
                    "No summary report found for department {Department}. Unable to send report notification",
                    JsonConvert.SerializeObject(department, Formatting.Indented));
                return;
            }
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "Failed to get summary report for department {Department}", JsonConvert.SerializeObject(department, Formatting.Indented));
            return;
        }

        SendNotificationsRequest notification;
        try
        {
            notification = CreateNotification(summaryReport, department);
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "Failed to create notification for department {DepartmentSapId} | Report {Report}", department.DepartmentSapId, JsonConvert.SerializeObject(summaryReport, Formatting.Indented));
            return;
        }

        var reportReceivers = department.ResourceOwnersAzureUniqueId.Concat(department.DelegateResourceOwnersAzureUniqueId).Distinct();

        foreach (var azureId in reportReceivers)
        {
            if (!_sendingNotificationEnabled)
            {
                logger.LogInformation("Sending of notifications is disabled. Skipping sending notification to user with AzureId {AzureId} for department {FullDepartmentName}", azureId, department.FullDepartmentName);
                continue;
            }

            try
            {
                var result = await notificationApiClient.SendNotification(notification, azureId);
                if (!result)
                    logger.LogCritical("Failed to send notification to user with AzureId {AzureId} | Report {Report}", azureId, JsonConvert.SerializeObject(summaryReport, Formatting.Indented));
            }
            catch (Exception e)
            {
                logger.LogCritical(e, "Failed to send notification to user with AzureId {AzureId} | Report {Report}", azureId, JsonConvert.SerializeObject(summaryReport, Formatting.Indented));
            }
        }
    }


    private SendNotificationsRequest CreateNotification(ApiWeeklySummaryReport report,
        ApiResourceOwnerDepartment department)
    {
        var personnelAllocationUri = $"{GetPortalUri()}apps/personnel-allocation/{department.DepartmentSapId}";
        var endingPositionsObjectList = report.PositionsEnding
            .Select(ep => new List<ListObject>
            {
                new()
                {
                    Value = ep.FullName,
                    Alignment = AdaptiveHorizontalAlignment.Left
                },
                new()
                {
                    Value = ep.EndDate == DateTime.MinValue
                        ? "No end date"
                        : $"End date: {ep.EndDate:dd/MM/yyyy}",
                    Alignment = AdaptiveHorizontalAlignment.Right
                }
            })
            .ToList();
        var personnelMoreThan100PercentObjectList = report.PersonnelMoreThan100PercentFTE
            .Select(ep => new List<ListObject>
            {
                new()
                {
                    Value = ep.FullName,
                    Alignment = AdaptiveHorizontalAlignment.Left
                },
                new()
                {
                    Value = $"{ep.FTE} %",
                    Alignment = AdaptiveHorizontalAlignment.Right
                }
            })
            .ToList();

        var averageTimeToHandleRequests = TimeSpan.TryParse(report.AverageTimeToHandleRequests, out var timeSpan)
            ? timeSpan.Days
            : int.Parse(report.AverageTimeToHandleRequests);

        var card = new AdaptiveCardBuilder()
            .AddHeading($"**Weekly summary - {department.FullDepartmentName}**")
            .AddTextRow(
                report.NumberOfPersonnel,
                "Number of personnel (employees and external hire)")
            .AddTextRow(
                report.CapacityInUse,
                "Capacity in use",
                "%")
            .AddTextRow(
                report.NumberOfRequestsLastPeriod,
                "New requests last week")
            .AddTextRow(
                report.NumberOfOpenRequests,
                "Open requests")
            .AddTextRow(
                report.NumberOfRequestsStartingInLessThanThreeMonths,
                "Requests with start date < 3 months")
            .AddTextRow(
                report.NumberOfRequestsStartingInMoreThanThreeMonths,
                "Requests with start date > 3 months")
            .AddTextRow(
                averageTimeToHandleRequests > 0
                    ? averageTimeToHandleRequests + " day(s)"
                    : "Less than a day",
                "Average time to handle request (last 12 months)")
            .AddTextRow(
                report.AllocationChangesAwaitingTaskOwnerAction,
                "Allocation changes awaiting task owner action")
            .AddTextRow(
                report.ProjectChangesAffectingNextThreeMonths,
                "Project changes last week affecting next 3 months")
            .AddListContainer("Allocations ending soon with no future allocation:", endingPositionsObjectList)
            .AddListContainer("Personnel with more than 100% workload:", personnelMoreThan100PercentObjectList)
            .AddNewLine()
            .AddActionButton("Go to Personnel allocation app", personnelAllocationUri)
            .Build();


        return new SendNotificationsRequest()
        {
            Title = $"Weekly summary - {department.FullDepartmentName}",
            EmailPriority = 1,
            Card = card,
            AppKey = "personnel-allocation",
            Description = $"Weekly report for department - {department.FullDepartmentName}"
        };
    }

    private string GetPortalUri()
    {
        var portalUri = configuration["Endpoints_portal"] ?? "https://fusion.equinor.com/";
        if (!portalUri.EndsWith("/"))
            portalUri += "/";
        return portalUri;
    }
}