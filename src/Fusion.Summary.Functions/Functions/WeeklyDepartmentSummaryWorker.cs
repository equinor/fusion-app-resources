using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Fusion.Integration.Profile;
using Fusion.Resources.Functions.Common.ApiClients;
using Fusion.Resources.Functions.Common.Extensions;
using Fusion.Summary.Functions.ReportCreator;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Fusion.Summary.Functions.Functions;

public class WeeklyDepartmentSummaryWorker
{
    private readonly IResourcesApiClient _resourceClient;
    private readonly ISummaryApiClient _summaryApiClient;
    private readonly ILogger<WeeklyDepartmentSummaryWorker> _logger;

    public WeeklyDepartmentSummaryWorker(IResourcesApiClient resourceClient, ILogger<WeeklyDepartmentSummaryWorker> logger, ISummaryApiClient summaryApiClient)
    {
        _resourceClient = resourceClient;
        _logger = logger;
        _summaryApiClient = summaryApiClient;
    }

    [FunctionName("weekly-department-summary-worker")]
    public async Task RunAsync(
        [ServiceBusTrigger("%department_summary_weekly_queue%", Connection = "AzureWebJobsServiceBus")]
        ServiceBusReceivedMessage message, ServiceBusMessageActions messageReceiver)
    {
        try
        {
            var dto = await JsonSerializer.DeserializeAsync<ApiResourceOwnerDepartment>(message.Body.ToStream());

            if (string.IsNullOrEmpty(dto.FullDepartmentName))
                throw new Exception("FullDepartmentIdentifier not valid.");

            await CreateAndStoreReportAsync(dto);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while processing message");
            throw;
        }
        finally
        {
            // Complete the message regardless of outcome.
            await messageReceiver.CompleteMessageAsync(message);
        }
    }

    private async Task CreateAndStoreReportAsync(ApiResourceOwnerDepartment message)
    {
        var departmentRequests = (await _resourceClient.GetAllRequestsForDepartment(message.FullDepartmentName)).ToList();

        var departmentPersonnel = (await _resourceClient.GetAllPersonnelForDepartment(message.FullDepartmentName))
            .Where(per =>
                per.AccountType != FusionAccountType.Consultant.ToString() &&
                per.AccountType != FusionAccountType.External.ToString())
            .ToList();

        // Check if the department has personnel, abort if not
        if (departmentPersonnel.Count == 0)
        {
            _logger.LogInformation("Department contains no personnel, no need to store report");
            return;
        }

        var report = await BuildSummaryReportAsync(departmentPersonnel, departmentRequests, message.DepartmentSapId);

        await _summaryApiClient.PutWeeklySummaryReportAsync(message.DepartmentSapId, report);
    }


    private static Task<ApiWeeklySummaryReport> BuildSummaryReportAsync(
        List<IResourcesApiClient.InternalPersonnelPerson> personnel,
        List<IResourcesApiClient.ResourceAllocationRequest> requests,
        string departmentSapId)
    {
        var report = new ApiWeeklySummaryReport()
        {
            Period = DateTime.UtcNow.GetPreviousWeeksMondayDate(),
            DepartmentSapId = departmentSapId,
            PositionsEnding = ResourceOwnerReportDataCreator
                .GetPersonnelPositionsEndingWithNoFutureAllocation(personnel)
                .Select(ep => new ApiEndingPosition()
                {
                    EndDate = ep.EndDate.GetValueOrDefault(DateTime.MinValue),
                    FullName = ep.FullName
                })
                .ToArray(),
            PersonnelMoreThan100PercentFTE = ResourceOwnerReportDataCreator
                .GetPersonnelAllocatedMoreThan100Percent(personnel)
                .Select(ep => new ApiPersonnelMoreThan100PercentFTE()
                {
                    FullName = ep.FullName,
                    FTE = ep.TotalWorkload
                })
                .ToArray(),
            NumberOfPersonnel = ResourceOwnerReportDataCreator.GetTotalNumberOfPersonnel(personnel).ToString(),
            CapacityInUse = ResourceOwnerReportDataCreator.GetCapacityInUse(personnel).ToString(),
            NumberOfRequestsLastPeriod = ResourceOwnerReportDataCreator.GetNumberOfRequestsLastWeek(requests).ToString(),
            NumberOfOpenRequests = ResourceOwnerReportDataCreator.GetNumberOfOpenRequests(requests).ToString(),
            NumberOfRequestsStartingInLessThanThreeMonths = ResourceOwnerReportDataCreator.GetNumberOfRequestsStartingInLessThanThreeMonths(requests).ToString(),
            NumberOfRequestsStartingInMoreThanThreeMonths = ResourceOwnerReportDataCreator.GetNumberOfRequestsStartingInMoreThanThreeMonths(requests).ToString(),
            AverageTimeToHandleRequests = ResourceOwnerReportDataCreator.GetAverageTimeToHandleRequests(requests).ToString(),
            AllocationChangesAwaitingTaskOwnerAction = ResourceOwnerReportDataCreator.GetAllocationChangesAwaitingTaskOwnerAction(requests).ToString(),
            ProjectChangesAffectingNextThreeMonths = ResourceOwnerReportDataCreator.CalculateDepartmentChangesLastWeek(personnel).ToString()
        };

        return Task.FromResult(report);
    }
}