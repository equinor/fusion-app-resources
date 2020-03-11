using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.Integration.Http.Handlers
{
    /// <summary>
    /// All http handlers should inherit from this one. That allows us to add general logging etc to all handlers.
    /// </summary>
    public class FunctionHttpMessageHandler : DelegatingHandler
    {
        protected readonly ILogger logger;
        private readonly ITokenProvider tokenProvider;
        private readonly IServiceDiscovery serviceDiscovery;

        public FunctionHttpMessageHandler(ILogger logger, ITokenProvider tokenProvider, IServiceDiscovery serviceDiscovery)
        {
            this.logger = logger;
            this.tokenProvider = tokenProvider;
            this.serviceDiscovery = serviceDiscovery;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage responseMessage;

            try
            {
                logger.LogInformation($"{request.Method} {request.RequestUri}");
                responseMessage = await base.SendAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogTrace($"{request.RequestUri} could not be executed: {ex.Message}");
                logger.LogError(ex, $"Error trying to send HTTP message to {request.RequestUri} - {ex.Message}");
                throw;
            }

            // If we were posting anything - log the body aswell.
            try
            {
                if (!responseMessage.IsSuccessStatusCode)
                {
                    switch (responseMessage.RequestMessage.Method)
                    {
                        case var p when p == HttpMethod.Post:
                        case var put when put == HttpMethod.Put:
                        case var patch when patch.ToString() == "PATCH":
                            var payload = responseMessage.RequestMessage.Content != null ?
                                await responseMessage.RequestMessage.Content.ReadAsStringAsync() :
                                "<empty>";

                            logger.LogTrace("Message body: " + payload);
                            break;
                    }

                    var contentLength = responseMessage.Content.Headers.ContentLength;

                    if (contentLength.GetValueOrDefault(long.MaxValue) < 1024 * 100 && responseMessage.Content.Headers.ContentType?.MediaType == "application/json")
                    {
                        var responseBody = await responseMessage.Content.ReadAsStringAsync();
                        logger.LogTrace("Response body: " + responseBody);
                    }
                    else if (contentLength.GetValueOrDefault(0) != 0)
                    {
                        logger.LogTrace($"Response body was too long to trace {responseMessage.Content.Headers.ContentLength} | {responseMessage.Content.Headers.ContentType}");
                    }
                    else
                    {
                        logger.LogTrace($"No response length header provided");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error while trying to track response: {ex.Message}");
            }

            return responseMessage;
        }

        protected async Task SetEndpointUriForRequestAsync(HttpRequestMessage request, ServiceEndpoint endpoint)
        {
            var url = await serviceDiscovery.ResolveServiceAsync(endpoint);
            var absoluteUri = UrlUtility.CombineUrl(url, request.RequestUri.PathAndQuery);
            request.RequestUri = new Uri(absoluteUri);
        }

        protected async Task AddAuthHeaderForRequestAsync(HttpRequestMessage request, string resource = null)
        {
            if (request.Headers.Authorization == null)
            {
                var token = resource is null ? await tokenProvider.GetAppAccessToken() : await tokenProvider.GetAppAccessToken(resource);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
    }

}
