#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fusion.Resources.Functions.ApiClients.ApiModels;
using Fusion.Resources.Functions.Integration;

namespace Fusion.Resources.Functions.ApiClients;

public class OrgClient : IOrgClient
{
    private readonly HttpClient orgClient;

    public OrgClient(IHttpClientFactory httpClientFactory)
    {
        orgClient = httpClientFactory.CreateClient(HttpClientNames.Application.Org);
        orgClient.Timeout = TimeSpan.FromMinutes(5);
    }

    public async Task<ApiChangeLog> GetChangeLog(string projectId, string positionId, string instanceId)
    {
        var data =
            await orgClient.GetAsJsonAsync<ApiChangeLog>($"/projects/{projectId}/positions/{positionId}/instances/{instanceId}/change-log?api-version=2.0");

        return data;
    }



}