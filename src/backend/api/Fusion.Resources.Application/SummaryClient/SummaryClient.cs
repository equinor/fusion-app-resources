using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Integration.Configuration;
using Fusion.Integration.Http.Models;
using Fusion.Resources.Application.SummaryClient.Models;
using Microsoft.Extensions.Configuration;

namespace Fusion.Resources.Application.SummaryClient;

internal class SummaryClient : ISummaryClient
{
    private readonly IFusionTokenProvider fusionTokenProvider;
    private readonly string scope;
    private readonly HttpClient summaryClient;

    public SummaryClient(IHttpClientFactory httpClientFactory, IFusionTokenProvider fusionTokenProvider, IConfiguration configuration)
    {
        this.fusionTokenProvider = fusionTokenProvider;
        summaryClient = httpClientFactory.CreateClient(HttpClientNames.Summary);
        scope = configuration["AzureAd:ClientId"] ?? throw new ArgumentException("ClientId not configured");
    }


    private async Task SetAuthToken()
    {
        // Should have internal caching so should be fast
        var token = await fusionTokenProvider.GetApplicationTokenAsync(scope);
        summaryClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<SummaryApiCollectionDto<ResourceOwnerWeeklySummaryReportDto>> GetSummaryReportForPeriodStartAsync(string departmentSapId, DateTime periodStart, CancellationToken cancellationToken = default)
    {
        await SetAuthToken();

        using var response = await summaryClient.GetAsync(
            $"/resource-owners-summary-reports/{departmentSapId}/weekly?$filter=period eq '{periodStart.Date:O}'", cancellationToken);


        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new SummaryIntegrationException($"Summary Api returned status code: {response.StatusCode} - {response.ReasonPhrase}", content);
        }


        var resultContent = await response.Content.ReadFromJsonAsync<SummaryApiCollectionDto<ResourceOwnerWeeklySummaryReportDto>>(cancellationToken: cancellationToken);

        return resultContent ?? throw new SummaryIntegrationException("Summary Api returned null content", "null");
    }

    public async Task<SummaryApiCollectionDto<ResourceOwnerWeeklySummaryReportDto>> GetSummaryReportsAsync(string departmentSapId, int? top, int? skip, CancellationToken cancellationToken = default)
    {
        var url = $"/resource-owners-summary-reports/{departmentSapId}/weekly";

        if (top != null || skip != null)
        {
            url += "?";
            if (top != null)
            {
                url += $"$top={top}";
            }

            if (skip != null)
            {
                url += $"&$skip={skip}";
            }
        }

        await SetAuthToken();
        using var response = await summaryClient.GetAsync(url, cancellationToken);


        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new SummaryIntegrationException($"Summary Api returned status code: {response.StatusCode} - {response.ReasonPhrase}", content);
        }


        var resultContent = await response.Content.ReadFromJsonAsync<SummaryApiCollectionDto<ResourceOwnerWeeklySummaryReportDto>>(cancellationToken: cancellationToken);

        return resultContent ?? throw new SummaryIntegrationException("Summary Api returned null content", "null");
    }
}