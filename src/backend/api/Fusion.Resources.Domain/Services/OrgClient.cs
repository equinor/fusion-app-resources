using Fusion.Integration.Configuration;
using Fusion.Integration;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Services;

public class OrgClient : IOrgClient
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly IFusionEndpointResolver _endpointResolver;
    private readonly IFusionTokenProvider _tokenProvider;

    public OrgClient(
        IHttpClientFactory clientFactory,
        IFusionTokenProvider tokenProvider,
        IFusionEndpointResolver endpointResolver)
    {
        _clientFactory = clientFactory;
        _tokenProvider = tokenProvider;
        _endpointResolver = endpointResolver;
    }

    private async Task<HttpResponseMessage> PutAsync(string url, string token, JObject data, int timeOutInSeconds = 100)
    {
        var httpClient = _clientFactory.CreateClient();

        httpClient.BaseAddress = new Uri(url);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        httpClient.Timeout = TimeSpan.FromSeconds(timeOutInSeconds);

        // Convert JObject to HttpContent
        HttpContent content = new StringContent(data.ToString(), Encoding.UTF8, "application/json");

        // Make the request
        return await httpClient.PutAsync(url, content);
    }

    private async Task<HttpResponseMessage> PatchAsync(string url, string token, JObject data, int timeOutInSeconds = 100)
    {
        var httpClient = _clientFactory.CreateClient();

        httpClient.BaseAddress = new Uri(url);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        httpClient.Timeout = TimeSpan.FromSeconds(timeOutInSeconds);

        // Convert JObject to HttpContent
        HttpContent content = new StringContent(data.ToString(), Encoding.UTF8, "application/json");

        // Make the request
        return await httpClient.PatchAsync(url, content);
    }

    public async Task<HttpResponseMessage> SavePosition(Guid projectId, Guid positionId, JObject obj, int timeoutInSeconds = 100)
    {
        var baseUrl = await _endpointResolver.ResolveEndpointAsync(FusionEndpoint.ProOrganisation);
        var url = $"{baseUrl}/projects/{projectId}/positions/{positionId}?api-version=2.0";

        var token = await _tokenProvider.GetApplicationTokenAsync();

        return await PutAsync(url, token, obj, timeoutInSeconds);
    }

    public async Task<HttpResponseMessage> UpdateFutureSplit(Guid projectId, Guid positionId, Guid positionInstanceId, JObject obj, int timeoutInSeconds = 100)
    {
        var baseUrl = await _endpointResolver.ResolveEndpointAsync(FusionEndpoint.ProOrganisation);
        var url = $"{baseUrl}/projects/{projectId}/positions/{positionId}/instances/{positionInstanceId}?api-version=2.0";

        var token = await _tokenProvider.GetApplicationTokenAsync();

        return await PatchAsync(url, token, obj, timeoutInSeconds);
    }

    public async Task<HttpResponseMessage> AllocateRequestInstance(Guid projectId, Guid draftId, Guid positionId, Guid positionInstanceId, JObject obj, int timeoutInSeconds = 100)
    {
        var baseUrl = await _endpointResolver.ResolveEndpointAsync(FusionEndpoint.ProOrganisation);
        var url = $"/projects/{projectId}/drafts/{draftId}/positions/{positionId}/instances/{positionInstanceId}?api-version=2.0";

        var token = await _tokenProvider.GetApplicationTokenAsync();

        return await PatchAsync(url, token, obj, timeoutInSeconds);
    }
}