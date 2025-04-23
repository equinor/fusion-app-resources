using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Integration;
using Fusion.Integration.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Fusion.Resources.Api.Configuration;

public class SummaryClientMessageHandler : DelegatingHandler
{
    private readonly IFusionTokenProvider fusionTokenProvider;
    private readonly IConfiguration configuration;
    private readonly string clientId;

    public SummaryClientMessageHandler(IFusionTokenProvider fusionTokenProvider, IOptions<FusionTokenOptions> options, IConfiguration configuration)
    {
        this.fusionTokenProvider = fusionTokenProvider;
        this.configuration = configuration;
        this.clientId = options.Value.ClientId;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var endpoint = configuration.GetValue<string?>("InternalServiceDiscovery:Summary:Url");
        if (string.IsNullOrEmpty(endpoint))
            throw new ArgumentException("Endpoint for Summary could not be resolved");

        request.RequestUri = new Uri(endpoint + request.RequestUri?.PathAndQuery);

        if (request.Headers.Authorization == null)
        {
            var token = await fusionTokenProvider.GetApplicationTokenAsync(clientId);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}