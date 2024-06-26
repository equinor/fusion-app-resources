using System.Text.Json;
using Fusion.Resources.Functions.Common.Integration.Http;
using Microsoft.AspNetCore.Http;

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
        // TODO: Parallelize or change to list in controller input

        foreach (var requestData in departments)
        {
            var body = new JsonContent(JsonSerializer.Serialize(requestData));
            using var response =
                await summaryClient.PutAsync($"departments/{requestData.DepartmentSapId}", body, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                // TODO: How to handle error
            }
        }
    }
}