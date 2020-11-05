using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.Integration.Http.Handlers
{
    public class GraphAppMessageHandler: FunctionHttpMessageHandler
    {
        public GraphAppMessageHandler(ILoggerFactory logger, ITokenProvider tokenProvider, IServiceDiscovery serviceDiscovery)
            : base(logger.CreateLogger<GraphAppMessageHandler>(), tokenProvider, serviceDiscovery)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Headers.Authorization == null)
            {
                var token = await tokenProvider.GetAppAccessToken("https://graph.microsoft.com");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}