using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.Common.Integration.Http.Handlers
{
    public class PeopleHttpHandler : FunctionHttpMessageHandler
    {
        private readonly IOptions<HttpClientsOptions> options;

        public PeopleHttpHandler(ILoggerFactory logger, ITokenProvider tokenProvider, IServiceDiscovery serviceDiscovery, IOptions<HttpClientsOptions> options)
            : base(logger.CreateLogger<PeopleHttpHandler>(), tokenProvider, serviceDiscovery)
        {
            this.options = options;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await SetEndpointUriForRequestAsync(request, ServiceEndpoint.People);
            await AddAuthHeaderForRequestAsync(request, options.Value.Fusion);

            return await base.SendAsync(request, cancellationToken);
        }
    }

}