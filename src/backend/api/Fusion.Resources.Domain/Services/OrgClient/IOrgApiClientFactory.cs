using System.Net.Http;
using Fusion.Integration;

namespace Fusion.Resources.Domain.Services.OrgClient;

public interface IOrgApiClientFactory
{
    public OrgApiClient CreateClient();
}

internal class OrgApiClientFactory : IOrgApiClientFactory
{
    private readonly IHttpClientFactory httpClientFactory;

    public OrgApiClientFactory(IHttpClientFactory httpClientFactory)
    {
        this.httpClientFactory = httpClientFactory;
    }

    public OrgApiClient CreateClient()
    {
        return new OrgApiClient(httpClientFactory.CreateClient(IntegrationConfig.HttpClients.ApplicationOrg()));
    }
}