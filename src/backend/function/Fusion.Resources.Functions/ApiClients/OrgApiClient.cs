#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fusion.Resources.Functions.ApiClients.ApiModels;
using Fusion.Resources.Functions.Integration;
using Microsoft.OData.Edm;

namespace Fusion.Resources.Functions.ApiClients;

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
}