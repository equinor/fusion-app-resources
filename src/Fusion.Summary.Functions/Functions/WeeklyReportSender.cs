using System;
using System.Threading.Tasks;
using Fusion.Resources.Functions.Common.ApiClients;
using Fusion.Resources.Functions.Common.ApiClients.ApiModels;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Fusion.Summary.Functions.Functions;

public class WeeklyReportSender
{
    private readonly ISummaryApiClient summaryApiClient;
    private readonly INotificationApiClient notificationApiClient;
    private readonly ILogger<WeeklyReportSender> logger;


    public WeeklyReportSender(ISummaryApiClient summaryApiClient, INotificationApiClient notificationApiClient,
        ILogger<WeeklyReportSender> logger)
    {
        this.summaryApiClient = summaryApiClient;
        this.notificationApiClient = notificationApiClient;
        this.logger = logger;
    }

    [FunctionName("weekly-report-sender")]
    public async Task RunAsync([TimerTrigger("0 0 8 * * 1", RunOnStartup = false)] TimerInfo timerInfo)
    {
        var departments = await summaryApiClient.GetDepartmentsAsync();


        var options = new ParallelOptions()
        {
            MaxDegreeOfParallelism = 10
        };

        await Parallel.ForEachAsync(departments, options, async (department, ct) =>
        {
            var summaryReport = await summaryApiClient.GetLatestWeeklyReportAsync(department.DepartmentSapId, ct);

            if (summaryReport is null)
            {
                logger.LogCritical(
                    "No summary report found for departmentSapId {@Department}. Unable to send report notification",
                    department);
                return;
            }

            var notification = CreateNotification(summaryReport);

            await notificationApiClient.SendNotification(notification, department.ResourceOwnerAzureUniqueId);
        });
    }


    private SendNotificationsRequest CreateNotification(ApiSummaryReport report)
    {
        throw new NotImplementedException();
    }
}