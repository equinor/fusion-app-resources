using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Application.Summary.Models;

namespace Fusion.Resources.Application.Summary;

internal class SummaryClient : ISummaryClient
{
    private readonly HttpClient summaryClient;

    public SummaryClient(IHttpClientFactory httpClientFactory)
    {
        summaryClient = httpClientFactory.CreateClient(HttpClientNames.Summary);
    }

    public async Task<ResourceOwnerWeeklySummaryReport?> GetSummaryReportForPeriodStartAsync(string departmentSapId, DateTime periodStart, CancellationToken cancellationToken = default)
    {
        var response = await summaryClient.GetAsync(
            $"/resource-owners-summary-reports/{departmentSapId}/weekly?$filter=period eq '{periodStart.Date:O}'", cancellationToken);


        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new SummaryIntegrationException($"Summary Api returned status code: {response.StatusCode} - {response.ReasonPhrase}", response, content);
        }


        var resultContent = await response.Content.ReadFromJsonAsync<SummaryApiCollection<ResourceOwnerWeeklySummaryReport>>(cancellationToken: cancellationToken);

        return resultContent?.Items.FirstOrDefault();
    }
}