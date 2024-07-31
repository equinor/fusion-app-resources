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

public class WeeklyReportSender
{
    private readonly ISummaryApiClient summaryApiClient;
    private readonly INotificationApiClient notificationApiClient;
    private readonly ILogger<WeeklyReportSender> logger;
    private readonly IConfiguration configuration;


    public WeeklyReportSender(ISummaryApiClient summaryApiClient, INotificationApiClient notificationApiClient,
        ILogger<WeeklyReportSender> logger, IConfiguration configuration)
    {
        this.summaryApiClient = summaryApiClient;
        this.notificationApiClient = notificationApiClient;
        this.logger = logger;
        this.configuration = configuration;
    }

    [FunctionName("weekly-report-sender")]
    public async Task RunAsync([TimerTrigger("0 0 8 * * 1", RunOnStartup = false)] TimerInfo timerInfo)
    {
        var departments = await summaryApiClient.GetDepartmentsAsync();

        if (departments is null)
        {
            logger.LogCritical("No departments found. Exiting");
            return;
        }

        var options = new ParallelOptions()
        {
            MaxDegreeOfParallelism = 10
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

            await notificationApiClient.SendNotification(notification, department.ResourceOwnerAzureUniqueId);
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
            .AddColumnSet(new AdaptiveCardColumn(
                report.NumberOfPersonnel,
                "Number of personnel (employees and external hire)"))
            .AddColumnSet(new AdaptiveCardColumn(
                report.CapacityInUse,
                "Capacity in use",
                "%"))
            .AddColumnSet(
                new AdaptiveCardColumn(
                    report.NumberOfRequestsLastPeriod,
                    "New requests last week"))
            .AddColumnSet(new AdaptiveCardColumn(
                report.NumberOfOpenRequests,
                "Open requests"))
            .AddColumnSet(new AdaptiveCardColumn(
                report.NumberOfRequestsStartingInLessThanThreeMonths,
                "Requests with start date < 3 months"))
            .AddColumnSet(new AdaptiveCardColumn(
                report.NumberOfRequestsStartingInMoreThanThreeMonths,
                "Requests with start date > 3 months"))
            .AddColumnSet(new AdaptiveCardColumn(
                averageTimeToHandleRequests > 0
                    ? averageTimeToHandleRequests + " day(s)"
                    : "Less than a day",
                "Average time to handle request (last 12 months)"))
            .AddColumnSet(new AdaptiveCardColumn(
                report.AllocationChangesAwaitingTaskOwnerAction,
                "Allocation changes awaiting task owner action"))
            .AddColumnSet(new AdaptiveCardColumn(
                report.ProjectChangesAffectingNextThreeMonths,
                "Project changes last week affecting next 3 months"))
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

