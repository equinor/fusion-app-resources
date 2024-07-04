using Fusion.Summary.Api.Controllers.ApiModels;
using Fusion.Summary.Api.Controllers.Requests;
using Fusion.Testing;

namespace Fusion.Summary.Api.Tests.Helpers;

public static class SummaryReportHelpers
{
    public static async Task<TestClientHttpResponse<ApiCollection<ApiWeeklySummaryReport>>>
        GetWeeklySummaryReportsAsync(
            this HttpClient client, string sapDepartmentId)
    {
        var response =
            await client.TestClientGetAsync<ApiCollection<ApiWeeklySummaryReport>>(
                $"summary-reports/{sapDepartmentId}/weekly");

        return response;
    }


    public static async Task<TestClientHttpResponse<object>> PutWeeklySummaryReportAsync(this HttpClient client,
        string sapDepartmentId,
        Action<PutWeeklySummaryReportRequest>? setup = null)
    {
        var request = new PutWeeklySummaryReportRequest
        {
            Period = DateTime.Parse("01.07.2024 12:23:00"),
            NumberOfPersonnel = "1",
            CapacityInUse = "2",
            NumberOfRequestsLastPeriod = "3",
            NumberOfOpenRequests = "4",
            NumberOfRequestsStartingInLessThanThreeMonths = "5",
            NumberOfRequestsStartingInMoreThanThreeMonths = "6",
            AverageTimeToHandleRequests = "7",
            AllocationChangesAwaitingTaskOwnerAction = "8",
            ProjectChangesAffectingNextThreeMonths = "9",
            PositionsEnding = Enumerable.Range(1, 5).Select(i =>
                {
                    return new ApiEndingPosition
                    {
                        EndDate = DateTime.Now,
                        FullName = i.ToString()
                    };
                })
                .ToArray(),
            PersonnelMoreThan100PercentFTE = Enumerable.Range(1, 5).Select(i =>
                {
                    return new ApiPersonnelMoreThan100PercentFTE()
                    {
                        FullName = i.ToString(),
                        FTE = i
                    };
                })
                .ToArray()
        };

        setup?.Invoke(request);

        return await client.TestClientPutAsync<object>($"summary-reports/{sapDepartmentId}/weekly", request);
    }
}