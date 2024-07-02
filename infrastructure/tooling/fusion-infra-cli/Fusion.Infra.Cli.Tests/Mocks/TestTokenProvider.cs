using Azure.Core;

namespace Fusion.Infra.Cli.Mocks
{
    public class TestTokenProvider : ITokenProvider
    {
        public Task<AccessToken> GetAccessToken(string resource)
        {
            return Task.FromResult(new AccessToken("[test token]", DateTimeOffset.Now));
        }
    }
}