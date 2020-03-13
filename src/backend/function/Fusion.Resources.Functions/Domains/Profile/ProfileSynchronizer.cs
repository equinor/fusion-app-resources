using System.Net.Http;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.Domains.Profile
{
    public class ProfileSynchronizer
    {
        private readonly HttpClient peopleClient;
        private readonly HttpClient resourcesClient;

        public ProfileSynchronizer(IHttpClientFactory httpClientFactory)
        {
            peopleClient = httpClientFactory.CreateClient(HttpClientNames.Application.People);
            resourcesClient = httpClientFactory.CreateClient(HttpClientNames.Application.Resources);
        }

        public async Task SynchronizeAsync()
        {
            //retrieve list of personell from resources api
            //

        }
    }
}
