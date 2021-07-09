using Fusion.Integration;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fusion.Resources.Application.People
{
    public class PeopleIntegration : IPeopleIntegration
    {
        private readonly HttpClient pplClient;

        public PeopleIntegration(IHttpClientFactory httpClientFactory)
        {
            this.pplClient = httpClientFactory.CreateClient(IntegrationConfig.HttpClients.ApplicationPeople());
        }

        public async Task UpdatePreferredContactMailAsync(Guid azureUniqueId, string? preferredContactMail)
        {
            var resp = await pplClient.PatchAsJsonAsync($"/persons/{azureUniqueId}/extended-profile?api-version=3.0", new
            {
                preferredContactMail = preferredContactMail
            });

            if (!resp.IsSuccessStatusCode)
            {
                var content = await resp.Content.ReadAsStringAsync();
                throw new PeopleIntegrationException($"Could not update preferred contact mail for user. People api returned status code {resp.StatusCode}", resp, content);
            }
        }
    }

}
