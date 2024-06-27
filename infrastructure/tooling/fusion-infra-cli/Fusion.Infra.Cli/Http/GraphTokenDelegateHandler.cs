// See https://aka.ms/new-console-template for more information
using System.Net.Http.Headers;

class GraphTokenDelegateHandler : DelegatingHandler
{
    private readonly ITokenProvider tokenProvider;

    public GraphTokenDelegateHandler(ITokenProvider tokenProvider)
    {
        this.tokenProvider = tokenProvider;
    }
    protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization == null)
        {
            var accessToken = await tokenProvider.GetAccessToken("https://graph.microsoft.com/.default");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Token);
        }

        var response = await base.SendAsync(request, cancellationToken);
        return response;
    }
}
