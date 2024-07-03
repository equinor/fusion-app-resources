// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;

class GraphTokenDelegateHandler : DelegatingHandler
{
    private readonly ILogger<GraphTokenDelegateHandler> logger;
    private readonly ITokenProvider tokenProvider;

    public GraphTokenDelegateHandler(ILogger<GraphTokenDelegateHandler> logger, ITokenProvider tokenProvider)
    {
        this.logger = logger;
        this.tokenProvider = tokenProvider;
    }

    protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization == null)
        {
            try
            {
                var accessToken = await tokenProvider.GetAccessToken("https://graph.microsoft.com/.default");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not aquire token to graph api: {Message}", ex.Message);
                throw;
            }
        }

        var response = await base.SendAsync(request, cancellationToken);
        return response;
    }
}
