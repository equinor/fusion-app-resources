using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.Integration.Http.Handlers
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
