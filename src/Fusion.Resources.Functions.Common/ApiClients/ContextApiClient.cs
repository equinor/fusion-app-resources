using Fusion.Resources.Functions.Common.ApiClients.ApiModels;
using Fusion.Resources.Functions.Common.Integration.Http;

namespace Fusion.Resources.Functions.Common.ApiClients;

public class ContextApiClient : IContextApiClient
{
    private readonly HttpClient client;

    public ContextApiClient(IHttpClientFactory httpClientFactory)
    {
        client = httpClientFactory.CreateClient(HttpClientNames.Application.Context);
    }

    public async Task<ICollection<ApiContext>> GetContextsAsync(string? contextType = null, CancellationToken cancellationToken = default)
    {
        var url = contextType is null ? "/contexts" : $"/contexts?$filter=type eq '{contextType}'";
        return await client.GetAsJsonAsync<ICollection<ApiContext>>(url, cancellationToken);
    }
}