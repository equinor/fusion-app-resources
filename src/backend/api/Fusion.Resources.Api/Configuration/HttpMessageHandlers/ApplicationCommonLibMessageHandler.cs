using Fusion.Integration;
using Fusion.Integration.Configuration;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Configuration
{
    public class ApplicationCommonLibMessageHandler : DelegatingHandler
    {
        private readonly IFusionEndpointResolver endpointResolver;
        private readonly IFusionTokenProvider fusionTokenProvider;

        public ApplicationCommonLibMessageHandler(IFusionEndpointResolver endpointResolver, IFusionTokenProvider fusionTokenProvider)
        {
            this.endpointResolver = endpointResolver;
            this.fusionTokenProvider = fusionTokenProvider;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var baseUri = new Uri(await endpointResolver.ResolveEndpointAsync(FusionEndpoint.CommonLib));
            var absoluteUri = new Uri(baseUri, request.RequestUri!.PathAndQuery);
            request.RequestUri = absoluteUri;

            if (request.Headers.Authorization == null)
            {
                var token = await fusionTokenProvider.GetApplicationTokenAsync();
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
