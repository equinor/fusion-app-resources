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
            const string appKey = FusionServiceEndpointKeys.CommonLib;
            var endpoint = await endpointResolver.ResolveEndpointAsync(appKey);

            if (endpoint is null)
                throw new ArgumentException($"Endpoint for {appKey} could not be resolved");

            // https://docs.fusion.equinor.com/blog/integration-lib-discovery
            // NOTE: Currently integration lib only supports passing one scope as parameter when requesting a token, even though the service endpoint can contain multiple scopes.
            // At the moment the services provided by Fusion only contains one scope and we can therefore safely use endpoint.Scopes[0].
            var scope = endpoint.Scopes[0];

            request.RequestUri = new Uri(endpoint + request.RequestUri?.PathAndQuery);

            if (request.Headers.Authorization == null)
            {
                var token = await fusionTokenProvider.GetApplicationTokenAsync(scope);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
