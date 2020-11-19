using Fusion.Resources.Functions.ApiClients;
using Fusion.Resources.Functions.Integration.Http;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.Functions.Notifications
{
    public class UrlResolver : IUrlResolver
    {
        private readonly IContextApiClient contextApiClient;
        private readonly IConfiguration configuration;

        public UrlResolver(IContextApiClient contextApiClient, IConfiguration configuration)
        {
            this.contextApiClient = contextApiClient;
            this.configuration = configuration;
        }

        public async Task<string> ResolveActiveRequestsAsync(IResourcesApiClient.ProjectContract projectContract)
        {
            var fusionBaseUrl = configuration.GetValue<string>("Endpoints_portal");
            var contextId = await contextApiClient.ResolveContextIdByExternalIdAsync(projectContract.ProjectId.ToString(), "OrgChart");

            var url = UrlUtility.CombineUrls(fusionBaseUrl, "apps", "resources", contextId.ToString(), projectContract.Id.ToString());

            return url;
        }
    }
}
