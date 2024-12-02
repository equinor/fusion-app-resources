using Fusion.Resources.Functions.Common.Integration.Authentication;
using Fusion.Resources.Functions.Common.Integration.ServiceDiscovery;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fusion.Resources.Functions.Common.Integration.Http.Handlers;

public class ContextHttpHandler : FunctionHttpMessageHandler
{
    private readonly IOptions<HttpClientsOptions> options;

    public ContextHttpHandler(ILoggerFactory logger, ITokenProvider tokenProvider, IServiceDiscovery serviceDiscovery, IOptions<HttpClientsOptions> options)
        : base(logger.CreateLogger<ContextHttpHandler>(), tokenProvider, serviceDiscovery)
    {
        this.options = options;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await SetEndpointUriForRequestAsync(request, ServiceEndpoint.Context);
        await AddAuthHeaderForRequestAsync(request, options.Value.Fusion);

        return await base.SendAsync(request, cancellationToken);
    }
}