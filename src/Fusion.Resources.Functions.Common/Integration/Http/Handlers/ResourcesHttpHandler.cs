using Fusion.Resources.Functions.Common.Integration.Authentication;
using Fusion.Resources.Functions.Common.Integration.ServiceDiscovery;
using Microsoft.Extensions.Logging;

namespace Fusion.Resources.Functions.Common.Integration.Http.Handlers
{
    public class ResourcesHttpHandler : FunctionHttpMessageHandler
    {

        public ResourcesHttpHandler(ILoggerFactory logger, ITokenProvider tokenProvider, IServiceDiscovery serviceDiscovery)
            : base(logger.CreateLogger<ResourcesHttpHandler>(), tokenProvider, serviceDiscovery)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await SetEndpointUriForRequestAsync(request, ServiceEndpoint.Resources);
            await AddAuthHeaderForRequestAsync(request);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
