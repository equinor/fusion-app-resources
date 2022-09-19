using Fusion.Resources.Functions.Integration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static Fusion.Resources.Functions.ApiClients.IResourcesApiClient;

namespace Fusion.Resources.Functions.ApiClients
{
    internal class ResourcesApiClient : IResourcesApiClient
    {
        private readonly HttpClient resourcesClient;
        private readonly ILogger<ResourcesApiClient> log;

        public ResourcesApiClient(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
        {
            resourcesClient = httpClientFactory.CreateClient(HttpClientNames.Application.Resources);
            log = loggerFactory.CreateLogger<ResourcesApiClient>();
        }

        public async Task<IEnumerable<ProjectReference>> GetProjectsAsync()
        {
            return await resourcesClient.GetAsJsonAsync<IEnumerable<ProjectReference>>("projects");
        }

        public async Task<IEnumerable<ResourceAllocationRequest>> GetIncompleteDepartmentAssignedResourceAllocationRequestsForProjectAsync(ProjectReference project)
        {
            var data = await resourcesClient.GetAsJsonAsync<InternalCollection<ResourceAllocationRequest>>(
                $"projects/{project.Id}/resources/requests/?$filter=state.IsComplete eq false and isDraft eq false&$expand=orgPosition,orgPositionInstance&$top={int.MaxValue}");

            return data.Value.Where(x => x.AssignedDepartment is not null);
        }

        public async Task<bool> ReassignRequestAsync(ResourceAllocationRequest item, string department)
        {
            var content = JsonConvert.SerializeObject(new { AssignedDepartment = department });
            var stringContent = new StringContent(content, Encoding.UTF8, "application/json");

            var result = await resourcesClient.PatchAsync($"/resources/requests/internal/{item.Id}", stringContent);

            if (result.IsSuccessStatusCode)
            {
                log.LogInformation($"Request {item.Id} reassigned successfully to {department}.");
                return true;
            }

            var exceptionMessage = await result.Content.ReadAsStringAsync();
            log.LogError(exceptionMessage);
            return false;
        }

        internal class InternalCollection<T>
        {
            public InternalCollection(IEnumerable<T> items)
            {
                Value = items;
            }

            public IEnumerable<T> Value { get; set; }
        }

    }
}
