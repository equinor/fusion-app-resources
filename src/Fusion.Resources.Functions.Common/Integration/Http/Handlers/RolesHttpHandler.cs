using Fusion.Resources.Functions.Common.Integration.Authentication;
using Fusion.Resources.Functions.Common.Integration.ServiceDiscovery;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fusion.Resources.Functions.Common.Integration.Http.Handlers;

public class RolesHttpHandler : FunctionHttpMessageHandler
{
    private readonly IOptions<HttpClientsOptions> options;

    public RolesHttpHandler(ILoggerFactory loggerFactory, ITokenProvider tokenProvider, IServiceDiscovery serviceDiscovery, IOptions<HttpClientsOptions> options)
        : base(loggerFactory.CreateLogger<RolesHttpHandler>(), tokenProvider, serviceDiscovery)
    {
        this.options = options;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await SetEndpointUriForRequestAsync(request, ServiceEndpoint.Roles);
        await AddAuthHeaderForRequestAsync(request, options.Value.Fusion);

        return await base.SendAsync(request, cancellationToken);
    }
}