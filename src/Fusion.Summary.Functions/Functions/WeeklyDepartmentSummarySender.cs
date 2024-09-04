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
using static Fusion.Summary.Functions.CardBuilder.AdaptiveCardBuilder;

namespace Fusion.Summary.Functions.Functions;

public class WeeklyDepartmentSummarySender
{
    private readonly ISummaryApiClient summaryApiClient;
    private readonly INotificationApiClient notificationApiClient;
    private readonly ILogger<WeeklyDepartmentSummarySender> logger;
    private readonly IConfiguration configuration;

    private int _maxDegreeOfParallelism;

    public WeeklyDepartmentSummarySender(ISummaryApiClient summaryApiClient, INotificationApiClient notificationApiClient,
        ILogger<WeeklyDepartmentSummarySender> logger, IConfiguration configuration)
    {
        this.summaryApiClient = summaryApiClient;
        this.notificationApiClient = notificationApiClient;
        this.logger = logger;
        this.configuration = configuration;

        _maxDegreeOfParallelism = int.TryParse(configuration["weekly-department-summary-sender-parallelism"], out var result) ? result : 10;
    }

    [FunctionName("weekly-department-summary-sender")]
    public async Task RunAsync([TimerTrigger("0 0 8 * * 1", RunOnStartup = false)] TimerInfo timerInfo)
    {
        var departments = await summaryApiClient.GetDepartmentsAsync();

        if (departments is null || !departments.Any())
        {
            logger.LogCritical("No departments found. Exiting");
            return;
        }

        var options = new ParallelOptions()
        {
            MaxDegreeOfParallelism = _maxDegreeOfParallelism
        };

        // Use Parallel.ForEachAsync to easily limit the number of parallel requests
        await Parallel.ForEachAsync(departments, options, async (department, ct) =>
        {
            var summaryReport = await summaryApiClient.GetLatestWeeklyReportAsync(department.DepartmentSapId, ct);

            if (summaryReport is null)
            {
                logger.LogCritical(
                    "No summary report found for department {@Department}. Unable to send report notification",
                    department);
                return;
            }

            SendNotificationsRequest notification;
            try
            {
                notification = CreateNotification(summaryReport, department);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to create notification for department {@Department}", department);
                throw;
            }

            var reportReceivers = department.ResourceOwnersAzureUniqueId.Concat(department.DelegateResourceOwnersAzureUniqueId).Distinct();

            foreach (var azureId in reportReceivers)
            {
                var result = await notificationApiClient.SendNotification(notification, azureId);
                if (!result)
                    logger.LogError("Failed to send notification to user with AzureId {AzureId} | Report {@ReportId}", azureId, summaryReport);
            }
        });
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