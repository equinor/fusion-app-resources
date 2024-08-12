using System.Text.Json;
using Fusion.Resources.Functions.Common.Integration.Http;

namespace Fusion.Resources.Functions.Common.ApiClients;

public class SummaryApiClient : ISummaryApiClient
{
    private readonly HttpClient summaryClient;

    private readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    public SummaryApiClient(IHttpClientFactory httpClientFactory)
    {
        summaryClient = httpClientFactory.CreateClient(HttpClientNames.Application.Summary);
        summaryClient.Timeout = TimeSpan.FromMinutes(2);
    }

    public async Task PutDepartmentAsync(ApiResourceOwnerDepartment department,
        CancellationToken cancellationToken = default)
    {
        using var body = new JsonContent(JsonSerializer.Serialize(department, jsonSerializerOptions));

        // Error logging is handled by http middleware => FunctionHttpMessageHandler
        using var _ = await summaryClient.PutAsync($"departments/{department.DepartmentSapId}", body,
            cancellationToken);
    }

    public async Task<ICollection<ApiResourceOwnerDepartment>?> GetDepartmentsAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await summaryClient.GetAsync("departments", cancellationToken);

        if (!response.IsSuccessStatusCode)
            return null;

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        return await JsonSerializer.DeserializeAsync<ICollection<ApiResourceOwnerDepartment>>(contentStream,
                   jsonSerializerOptions,
                   cancellationToken: cancellationToken)
               ?? Array.Empty<ApiResourceOwnerDepartment>();
    }

    public async Task<ApiWeeklySummaryReport?> GetLatestWeeklyReportAsync(string departmentSapId,
        CancellationToken cancellationToken = default)
    {
        // Get the date of the last monday or today if today is monday
        // So the weekly report is based on the week that has passed
        var lastMonday = GetCurrentOrLastMondayDate();

        var queryString =
            $"resource-owners-summary-reports/{departmentSapId}/weekly?$filter=Period eq '{lastMonday.Date:O}'&$top=1";

        using var response = await summaryClient.GetAsync(queryString, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        return (await JsonSerializer.DeserializeAsync<ApiCollection<ApiWeeklySummaryReport>>(contentStream,
            jsonSerializerOptions,
            cancellationToken: cancellationToken))?.Items?.FirstOrDefault();
    }

    public async Task PutWeeklySummaryReportAsync(string departmentSapId, ApiWeeklySummaryReport report,
        CancellationToken cancellationToken = default)
    {
        using var body = new JsonContent(JsonSerializer.Serialize(report, jsonSerializerOptions));

        // Error logging is handled by http middleware => FunctionHttpMessageHandler
        using var _ = await summaryClient.PutAsync($"resource-owners-summary-reports/{departmentSapId}/weekly", body,
            cancellationToken);
    }

    private static DateTime GetCurrentOrLastMondayDate()
    {
        var date = DateTime.UtcNow;
        switch (date.DayOfWeek)
        {
            case DayOfWeek.Sunday:
                return date.AddDays(-6);
            case DayOfWeek.Monday:
                return date;
            case DayOfWeek.Tuesday:
            case DayOfWeek.Wednesday:
            case DayOfWeek.Thursday:
            case DayOfWeek.Friday:
            case DayOfWeek.Saturday:
            default:
            {
                var daysUntilMonday = (int)date.DayOfWeek - 1;

                return date.AddDays(-daysUntilMonday);
            }
        }
    }
}