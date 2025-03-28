using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Integration.Http;
using Fusion.Integration.Http.Models;
using Fusion.Integration.Org;
using Fusion.Resources.Domain.Services.OrgClient.Abstractions;
using Fusion.Services.Org.ApiModels;
using Newtonsoft.Json;

namespace Fusion.Resources.Domain.Services.OrgClient;

public class OrgApiClient : IOrgApiClient
{
    public OrgApiClient(HttpClient httpClient)
    {
        this.HttpClient = httpClient;
    }

    /// <summary>
    ///     Internal HttpClient used by the org client
    /// </summary>
    public HttpClient HttpClient { get; }

    public virtual Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        if (OrgClientRequestHeadersScope.Current.Value != null)
        {
            foreach (var header in OrgClientRequestHeadersScope.Current.Value.Headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }
        }

        return HttpClient.SendAsync(request, cancellationToken);
    }

    public async Task<ApiPositionV2> GetPositionV2Async(OrgProjectId projectId, Guid positionId, ODataQuery? query = null)
    {
        var url = ODataQuery.ApplyQueryString($"/projects/{projectId}/positions/{positionId}?api-version=2.0", query);
        var response = await this.GetAsync<ApiPositionV2>(url);

        if (!response.IsSuccessStatusCode)
        {
            throw new OrgApiError(response.Response, response.Content);
        }

        return response.Value;
    }
}