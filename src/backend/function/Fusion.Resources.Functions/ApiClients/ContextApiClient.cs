using Fusion.Resources.Functions.Integration;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.ApiClients
{
    public class ContextApiClient : IContextApiClient
    {
        private readonly HttpClient client;

        public ContextApiClient(IHttpClientFactory httpClientFactory)
        {
            client = httpClientFactory.CreateClient(HttpClientNames.Application.Context);
        }

        public async Task<Guid?> ResolveContextIdByExternalId(string externalId, string contextType = null)
        {
            var url = $"contexts?$filter=externalId eq '{externalId}'";

            if (!string.IsNullOrEmpty(contextType))
                url += $" && type eq '{contextType}'";

            var results = await client.GetAsJsonAsync(url, new[] { new { Id = Guid.Empty } });

            if (results.Length == 0)
                return null;

            if (results.Length > 1)
                throw new InvalidOperationException($"Located multiple contexts ({results.Length}) for externalId '{externalId}'.{(contextType == null ? " Maybe refine the search by using type filter" : "")}");

            var id = results.First().Id;

            return id;
        }
    }
}
