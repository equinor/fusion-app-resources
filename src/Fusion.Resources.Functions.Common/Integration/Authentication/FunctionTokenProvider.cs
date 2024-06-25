using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace Fusion.Resources.Functions.Integration.Authentication;

internal class FunctionTokenProvider : ITokenProvider
{
    private readonly IConfidentialClientApplication _app;

    public FunctionTokenProvider(IOptions<AuthOptions> optionsAccessor)
    {
        var options = optionsAccessor.Value;

        _app = ConfidentialClientApplicationBuilder.Create(options.ClientId)
            .WithClientSecret(options.Secret)
            .WithAuthority(AzureCloudInstance.AzurePublic, options.TenantId)
            .Build();
    }

    public async Task<string> GetAppAccessToken()
    {
        var scopes = new string[] { $"{_app.AppConfig.ClientId}/.default" };
        var clientToken = await _app.AcquireTokenForClient(scopes).ExecuteAsync();
        
        return clientToken.AccessToken;
    }

    public async Task<string> GetAppAccessToken(string resource)
    {
        var scopes = new string[] { $"{resource}/.default" };
        var clientToken =  await _app.AcquireTokenForClient(scopes).ExecuteAsync();

        return clientToken.AccessToken;
    }
}