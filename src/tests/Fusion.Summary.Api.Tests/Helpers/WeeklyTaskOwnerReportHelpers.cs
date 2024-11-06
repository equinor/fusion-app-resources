using Fusion.Summary.Api.Controllers.ApiModels;
using Fusion.Summary.Api.Controllers.Requests;
using Fusion.Testing;

namespace Fusion.Summary.Api.Tests.Helpers;

public static class WeeklyTaskOwnerReportHelpers
{
    public static async Task<TestClientHttpResponse<ApiCollection<ApiWeeklyTaskOwnerReport>>>
        GetWeeklyTaskOwnerReportsAsync(
            this HttpClient client, string projectId)
    {
        var response =
            await client.TestClientGetAsync<ApiCollection<ApiWeeklyTaskOwnerReport>>(
                $"projects/{projectId}/task-owners-summary-reports/weekly");

        return response;
    }

    public static async Task<TestClientHttpResponse<ApiWeeklyTaskOwnerReport>> PutWeeklyTaskOwnerReportAsync(this HttpClient client,
        string projectId, Action<PutWeeklyTaskOwnerReportRequest>? setup = null)
    {
        var now = CreateDayOfWeek(DayOfWeek.Monday);
        var request = new PutWeeklyTaskOwnerReportRequest()
        {
            PeriodStart = now,
            PeriodEnd = now.AddDays(7),
            ActionsAwaitingTaskOwnerAction = 1,
            PositionAllocationsEndingInNextThreeMonths = Enumerable.Range(1, 5).Select(i => new ApiPositionAllocationEnding
            {
                PositionName = i.ToString(),
                PositionNameDetailed = i.ToString(),
                PositionAppliesTo = now.AddDays(20),
                PositionExternalId = i.ToString()
            }).ToArray(),
            AdminAccessExpiringInLessThanThreeMonths = Enumerable.Range(1, 5).Select(i => new ApiAdminAccessExpiring
            {
                AzureUniqueId = Guid.NewGuid(),
                FullName = i.ToString(),
                Expires = now.AddDays(20)
            }).ToArray(),
            TBNPositionsStartingInLessThanThreeMonths = Enumerable.Range(6, 5).Select(i => new ApiTBNPositionStartingSoon
            {
                PositionName = i.ToString(),
                PositionNameDetailed = i.ToString(),
                PositionAppliesFrom = now.AddDays(20),
                PositionExternalId = i.ToString()
            }).ToArray()
        };

        setup?.Invoke(request);

        return await client.TestClientPutAsync<ApiWeeklyTaskOwnerReport>($"projects/{projectId}/task-owners-summary-reports/weekly",
            request);
    }

    private static DateTime CreateDayOfWeek(DayOfWeek dayOfWeek)
    {
        var newDate = DateTime.UtcNow;

        var daysUntil = ((int)dayOfWeek - (int)newDate.DayOfWeek + 7) % 7;

        return newDate.AddDays(daysUntil);
    }
}