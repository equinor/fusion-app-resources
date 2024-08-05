#nullable enable
using System.Text;
using Fusion.Resources.Functions.Common.Integration.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static Fusion.Resources.Functions.Common.ApiClients.IResourcesApiClient;

namespace Fusion.Resources.Functions.Common.ApiClients
{
    internal class ResourcesApiClient : IResourcesApiClient
    {
        private readonly HttpClient resourcesClient;
        private readonly ILogger<ResourcesApiClient> log;

        public ResourcesApiClient(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
        {
            resourcesClient = httpClientFactory.CreateClient(HttpClientNames.Application.Resources);
            resourcesClient.Timeout = System.TimeSpan.FromMinutes(5);
            log = loggerFactory.CreateLogger<ResourcesApiClient>();
        }

        public async Task<IEnumerable<ProjectReference>> GetProjectsAsync()
        {
            return await resourcesClient.GetAsJsonAsync<IEnumerable<ProjectReference>>("projects");
        }

        public async Task<IEnumerable<ResourceAllocationRequest>>
            GetIncompleteDepartmentAssignedResourceAllocationRequestsForProjectAsync(ProjectReference project)
        {
            var data = await resourcesClient.GetAsJsonAsync<InternalCollection<ResourceAllocationRequest>>(
                $"projects/{project.Id}/resources/requests/?$filter=state.IsComplete eq false and isDraft eq false&$expand=orgPosition,orgPositionInstance&$top={int.MaxValue}");

            return data.Value.Where(x => x.AssignedDepartment is not null);
        }

        public async Task<IEnumerable<ResourceAllocationRequest>> GetAllRequestsForDepartment(
            string departmentIdentifier)
        {
            try
            {
                var response = await resourcesClient.GetAsJsonAsync<InternalCollection<ResourceAllocationRequest>>(
                    $"departments/{departmentIdentifier}/resources/requests?$expand=orgPosition,orgPositionInstance,actions&$top=2000");

                return response.Value.ToList();
            }
            catch(Exception ex)
            {
                log.LogError($"Error getting requests for department '{departmentIdentifier}'", ex);

                throw;
            }
        }

        public async Task<IEnumerable<InternalPersonnelPerson>> GetAllPersonnelForDepartment(
            string departmentIdentifier)
        {
            try
            {
                var response = await resourcesClient.GetAsJsonAsync<InternalCollection<InternalPersonnelPerson>>(
           $"departments/{departmentIdentifier}/resources/personnel?api-version=2.0&$includeCurrentAllocations=true");

                return response.Value.ToList();
            }
            catch (Exception ex)
            {
                log.LogError($"Error getting personnel for department '{departmentIdentifier}'", ex);

                throw;
            }
        }

        public async Task<IEnumerable<ApiPersonAbsence>> GetLeaveForPersonnel(string personId)
        {
            var response = await resourcesClient.GetAsJsonAsync<InternalCollection<ApiPersonAbsence>>(
                $"persons/{personId}/absence");

            return response.Value.ToList();
        }

        public async Task<bool> ReassignRequestAsync(ResourceAllocationRequest item, string? department)
        {
            var content = JsonConvert.SerializeObject(new { AssignedDepartment = department });
            var stringContent = new StringContent(content, Encoding.UTF8, "application/json");

            var result = await resourcesClient.PatchAsync($"/projects/{item.OrgPosition!.ProjectId}/requests/{item.Id}",
                stringContent);

            if (result.IsSuccessStatusCode)
            {
                log.LogInformation($"Request {item.Id} reassigned successfully to {department}.");
                return true;
            }

            var exceptionMessage = await result.Content.ReadAsStringAsync();
            log.LogError(exceptionMessage);
            return false;
        }

        public async Task<IEnumerable<DelegatedresponsibleResult>> GetDelegatedResponsibleForDepartment(string departmentIdentifier)
        {
            var response = await resourcesClient.GetAsJsonAsync<InternalCollection<DelegatedresponsibleResult>>($"departments/{departmentIdentifier}/delegated-resource-owners");

            return response.Value;
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