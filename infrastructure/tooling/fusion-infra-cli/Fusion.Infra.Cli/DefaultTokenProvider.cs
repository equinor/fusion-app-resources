// See https://aka.ms/new-console-template for more information
using Azure.Core;
using Azure.Identity;

public class DefaultTokenProvider : ITokenProvider
{
    public async Task<AccessToken> GetAccessToken(string resource)
    {
        var credentials = new DefaultAzureCredential();
        var token = await credentials.GetTokenAsync(new TokenRequestContext(new[] { resource }), CancellationToken.None);

        return token;
    }
}