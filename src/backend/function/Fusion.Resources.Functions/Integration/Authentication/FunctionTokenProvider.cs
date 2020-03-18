using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.Integration.Authentication
{

    internal class FunctionTokenProvider : ITokenProvider
    {
        private readonly string clientid;
        private readonly string authority;
        private readonly string secret;
        private readonly TokenCache appTokenCache;

        static FunctionTokenProvider()
        {
            LoggerCallbackHandler.UseDefaultLogging = false;
        }

        ClientCredential Credentials
        {
            get
            {
                return new ClientCredential(clientid, secret);
            }
        }

        public FunctionTokenProvider(IOptions<AuthOptions> optionsAccessor)
        {
            var options = optionsAccessor.Value;

            authority = $"https://login.microsoftonline.com/{options.TenantId}";
            clientid = options.ClientId;
            secret = options.Secret;

            appTokenCache = new TokenCache();
        }

        public async Task<string> GetAppAccessToken()
        {
            var authContext = new AuthenticationContext(authority, appTokenCache);
            var authenticationResult = await authContext.AcquireTokenAsync(clientid, Credentials);

            return authenticationResult.AccessToken;
        }

        public async Task<string> GetAppAccessToken(string resource)
        {
            var authContext = new AuthenticationContext(authority, appTokenCache);
            var authenticationResult = await authContext.AcquireTokenAsync(resource, Credentials);

            return authenticationResult.AccessToken;
        }


    }

}
