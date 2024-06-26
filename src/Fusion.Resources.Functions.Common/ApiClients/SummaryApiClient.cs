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
}