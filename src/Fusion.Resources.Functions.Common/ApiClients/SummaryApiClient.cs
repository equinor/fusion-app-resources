using System.Text.Json;
using Fusion.Resources.Functions.Common.Integration.Http;

namespace Fusion.Resources.Functions.Common.ApiClients;


public class SummaryApiClient : ISummaryApiClient
{
    private readonly HttpClient summaryClient;

    public SummaryApiClient(IHttpClientFactory httpClientFactory)
    {
        summaryClient = httpClientFactory.CreateClient(HttpClientNames.Application.Summary);
        summaryClient.Timeout = TimeSpan.FromMinutes(2);
    }

    public async Task PutDepartmentsAsync(IEnumerable<ApiResourceOwnerDepartments> departments,
        CancellationToken cancellationToken = default)
    {
        var parallelOptions = new ParallelOptions()
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = 10,
        };

        await Parallel.ForEachAsync(departments, parallelOptions, async (ownerDepartments, token) =>
        {
            var body = new JsonContent(JsonSerializer.Serialize(ownerDepartments));

            // Error logging is handled by http middleware => FunctionHttpMessageHandler
            using var _ = await summaryClient.PutAsync($"departments/{ownerDepartments.DepartmentSapId}", body,
                cancellationToken);
        });
    }

    public async Task<ICollection<ApiResourceOwnerDepartments>> GetDepartmentsAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await summaryClient.GetAsync("departments", cancellationToken);

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        return await JsonSerializer.DeserializeAsync<ICollection<ApiResourceOwnerDepartments>>(contentStream,
                   cancellationToken: cancellationToken)
               ?? Array.Empty<ApiResourceOwnerDepartments>();
    }

    public async Task<ApiSummaryReport?> GetLatestWeeklyReportAsync(string departmentSapId,
        CancellationToken cancellationToken = default)
    {
        var queryString = $"summary-reports/{departmentSapId}?$filter=PeriodType eq 'Weekly'&$top=1";

        using var response = await summaryClient.GetAsync(queryString, cancellationToken);

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        return (await JsonSerializer.DeserializeAsync<ICollection<ApiSummaryReport>>(contentStream,
            cancellationToken: cancellationToken))?.FirstOrDefault();
    }
}