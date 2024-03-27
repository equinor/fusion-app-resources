using Fusion.Integration.Configuration;
using Fusion.Integration;
using System;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Services;

public class OrgHttpClient : IOrgHttpClient
{
    private readonly IFusionEndpointResolver _endpointResolver;
    private readonly IFusionTokenProvider _tokenProvider;
    private readonly HttpClient _httpClient;

    public OrgHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<RequestResponse<T>> SendAsync<T>(HttpRequestMessage request)
    {
        var baseUrl = await _endpointResolver.ResolveEndpointAsync(FusionEndpoint.ProOrganisation);
        var token = await _tokenProvider.GetApplicationTokenAsync();

        request.RequestUri = new Uri(new Uri(baseUrl), request.RequestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);

        return await RequestResponse<T>.FromResponseAsync(response);
    }
}
