using Fusion.ApiClients.Org;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Services
{
    internal class PeopleCompanyResolver : ICompanyResolver, IDisposable
    {
        private readonly HttpClient client;


        private List<ApiCompanyV2>? companies = null;
        private readonly Timer? cacheRestTimer = null;

        public PeopleCompanyResolver(IHttpClientFactory httpClientFactory)
        {
            this.client = httpClientFactory.CreateClient("FusionClient.People.Application");
            
            cacheRestTimer = new Timer((state) => companies = null, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        public void Dispose()
        {
            cacheRestTimer?.Dispose();
        }

        public async Task<ApiCompanyV2?> FindCompanyAsync(Guid companyId)
        {
            if (companies == null)
            {
                var response = await client.GetAsync("/companies");
                var content = await response.Content.ReadAsStringAsync();

                companies = JsonConvert.DeserializeObject<List<ApiCompanyV2>>(content)!;
            }

            return companies.FirstOrDefault(c => c.Id == companyId);
        }
    }
}
