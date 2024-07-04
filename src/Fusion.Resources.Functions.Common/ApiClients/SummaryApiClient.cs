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

    public async Task PutDepartmentsAsync(IEnumerable<ApiResourceOwnerDepartment> departments,
        CancellationToken cancellationToken = default)
    {
        var parallelOptions = new ParallelOptions()
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = 10,
        };

        await Parallel.ForEachAsync(departments, parallelOptions, async (ownerDepartments, token) =>
        {
            var body = new JsonContent(JsonSerializer.Serialize(ownerDepartments, jsonSerializerOptions));

            // Error logging is handled by http middleware => FunctionHttpMessageHandler
            using var _ = await summaryClient.PutAsync($"departments/{ownerDepartments.DepartmentSapId}", body,
                cancellationToken);
        });
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

    private static DateTime GetLastSunday(DateTime date)
    {
        var daysUntilSunday = (int)date.DayOfWeek;
        return date.AddDays(-daysUntilSunday);
    }

    public async Task<ApiWeeklySummaryReport?> GetLatestWeeklyReportAsync(string departmentSapId,
        CancellationToken cancellationToken = default)
    {
        var lastSunday = GetLastSunday(DateTime.UtcNow);

        var queryString =
            $"summary-reports/{departmentSapId}/weekly?$filter=Period eq '{lastSunday.Date:O}'&$top=1";

        using var response = await summaryClient.GetAsync(queryString, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        return (await JsonSerializer.DeserializeAsync<ApiCollection<ApiWeeklySummaryReport>>(contentStream,
            jsonSerializerOptions,
            cancellationToken: cancellationToken))?.Items?.FirstOrDefault();
    }
}