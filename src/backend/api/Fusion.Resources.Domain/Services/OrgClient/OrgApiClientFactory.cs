using System.Net.Http;
using Fusion.Integration;
using Fusion.Resources.Domain.Services.OrgClient.Abstractions;

namespace Fusion.Resources.Domain.Services.OrgClient;

public class OrgApiClientFactory : IOrgApiClientFactory
{
    private readonly IHttpClientFactory httpClientFactory;

    public OrgApiClientFactory(IHttpClientFactory httpClientFactory)
    {
        this.httpClientFactory = httpClientFactory;
    }

    public IOrgApiClient CreateClient()
    {
        return new OrgApiClient(httpClientFactory.CreateClient(IntegrationConfig.HttpClients.ApplicationOrg()));
    }
}