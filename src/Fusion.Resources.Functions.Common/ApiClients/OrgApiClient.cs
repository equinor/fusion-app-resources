using Fusion.Resources.Functions.Common.Integration.Http;
using Fusion.Services.Org.ApiModels;

namespace Fusion.Resources.Functions.Common.ApiClients;

public class OrgClient : IOrgClient
{
    private readonly HttpClient orgClient;

    public OrgClient(IHttpClientFactory httpClientFactory)
    {
        orgClient = httpClientFactory.CreateClient(HttpClientNames.Application.Org);
        orgClient.Timeout = TimeSpan.FromMinutes(5);
    }

    public async Task<ApiChangeLog> GetChangeLog(string projectId, DateTime timestamp)
    {
        var url = $"/projects/{projectId}/change-log?$filter=timestamp gt '{timestamp.ToString("yyyy-MM-dd")}'&api-version=2.0";
        var data =
            await orgClient.GetAsJsonAsync<ApiChangeLog>(
                url);

        return data;
    }

    public Task<List<ApiProjectV2>> GetProjectsAsync(ODataQuery? query = null, CancellationToken cancellationToken = default)
    {
        var url = ODataQuery.ApplyQueryString("/projects", query);
        return orgClient.GetAsJsonAsync<List<ApiProjectV2>>(url, cancellationToken: cancellationToken);
    }
}