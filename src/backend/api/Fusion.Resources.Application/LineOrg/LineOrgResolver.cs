using Fusion.Integration;
using Fusion.Resources.Application.LineOrg.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Application.LineOrg
{
    public class LineOrgResolver : ILineOrgResolver
    {
        private readonly IMemoryCache cache;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IFusionProfileResolver profileResolver;

        public LineOrgResolver(/*IMemoryCache cache, */IHttpClientFactory httpClientFactory,
                IFusionProfileResolver profileResolver)
        {
            //this.cache = cache;
            this.httpClientFactory = httpClientFactory;
            this.profileResolver = profileResolver;
        }

        public async Task<List<LineOrgDepartment>> GetResourceOwners(List<string> departmentIds, string? filter, CancellationToken cancellationToken)
        {
            var result = new List<LineOrgDepartment>();
            var managedDepartments = new HashSet<string>(departmentIds);

            var client = httpClientFactory.CreateClient("lineorg");

            var uri = "/lineorg/persons?top=2000&$filter=isresourceowner eq true";

            if (!string.IsNullOrEmpty(filter))
                uri += $"&$search={filter}";

            do
            {
                var response = await client.GetAsync(uri, cancellationToken);
                response.EnsureSuccessStatusCode();

                var page = JsonSerializer.Deserialize<PaginatedResponse<ProfileWithDepartment>>(
                    await response.Content.ReadAsStringAsync(cancellationToken),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                uri = page?.NextPage;
                var resourceOwners = page!.Value;

                var profiles = await profileResolver.ResolvePersonsAsync(
                    resourceOwners.Select(r => new Integration.Profile.PersonIdentifier(r.AzureUniqueId))
                );

                var resolvedProfiles = profiles
                    .Where(p => p.Success)
                    .ToDictionary(p => p.Profile.AzureUniqueId);


                foreach (var resourceOwner in resourceOwners)
                {
                    if (!managedDepartments.Contains(resourceOwner.FullDepartment)) continue;
                    if (!resolvedProfiles.ContainsKey(resourceOwner.AzureUniqueId)) continue;

                    var department = new LineOrgDepartment(resourceOwner.FullDepartment)
                    {
                        Responsible = resolvedProfiles[resourceOwner.AzureUniqueId].Profile
                    };

                    result.Add(department);
                }
            } while (!string.IsNullOrEmpty(uri));

            return result;
        }
    }
}
