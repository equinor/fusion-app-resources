using Fusion.Resources.Functions.Integration;
using Fusion.Resources.Functions.Telemetry;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
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
        private readonly TelemetryClient telemetryClient;

        public ResourcesApiClient(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, TelemetryClient telemetryClient)
        {
            resourcesClient = httpClientFactory.CreateClient(HttpClientNames.Application.Resources);
            log = loggerFactory.CreateLogger<ResourcesApiClient>();
            this.telemetryClient = telemetryClient;
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


        public async Task<List<ProjectContract>> GetProjectContractsAsync()
        {
            var operation = telemetryClient.StartOperation<DependencyTelemetry>("Get project contracts");
            operation.Telemetry.Type = "ASYNC";

            try
            {
                var projectResponse = await resourcesClient.GetAsync($"projects");
                var body = await projectResponse.Content.ReadAsStringAsync();

                if (!projectResponse.IsSuccessStatusCode)
                {
                    telemetryClient.TrackCritical($"Failed to retrieve projects from Resources API");
                    throw new ApiError(projectResponse.RequestMessage.RequestUri.ToString(), projectResponse.StatusCode, body, $"Failed to retrieve projects from Resources API");
                }

                var projectList = JsonConvert.DeserializeAnonymousType(body, new[] { new { Id = Guid.Empty, Name = string.Empty } }); //maps to API model ApiProjectReference in Resources API
                var projectContracts = new List<ProjectContract>();

                foreach (var project in projectList)
                {
                    var contractResponse = await resourcesClient.GetAsync($"projects/{project.Id}/contracts");
                    body = await contractResponse.Content.ReadAsStringAsync();

                    if (!contractResponse.IsSuccessStatusCode)
                    {
                        log.LogWarning($"Failed to retrieve contracts for project '{project.Id}' from Resources API [{projectResponse.StatusCode}]. Body: {body.Substring(0, 500)}. " +
                            $"Skipping notifications for this project.");
                        continue;
                    }

                    var contractList = JsonConvert.DeserializeAnonymousType(body, new { value = new List<ProjectContract>() });
                    contractList.value.ForEach(c =>
                    {
                        c.ProjectId = project.Id;
                        c.ProjectName = project.Name;
                    });

                    projectContracts.AddRange(contractList.value);
                }

                return projectContracts;
            }
            catch (Exception ex)
            {
                operation.Telemetry.Success = false;
                telemetryClient.TrackException(ex);

                throw;
            }
            finally
            {
                telemetryClient.StopOperation(operation);
            }
        }

        public async Task<PersonnelRequestList> GetTodaysContractRequests(ProjectContract projectContract, string state)
        {
            var odataFilter = $"$filter=lastActivity gt {DateTime.Today} and state eq '{state}'";
            var requestsResponse = await resourcesClient.GetAsync($"projects/{projectContract.ProjectId}/contracts/{projectContract.Id}/resources/requests?{odataFilter}");

            if (!requestsResponse.IsSuccessStatusCode)
            {
                log.LogWarning($"Failed to retrieve active requests for contract '{projectContract.Id}' in project '{projectContract.ProjectId}'");
                return new PersonnelRequestList();
            }

            var body = await requestsResponse.Content.ReadAsStringAsync();
            var requestList = JsonConvert.DeserializeObject<PersonnelRequestList>(body);

            return requestList;
        }

        public async Task<List<DelegatedRole>> RetrieveDelegatesForContractAsync(ProjectContract projectContract)
        {
            var delegatesResponse = await resourcesClient.GetAsync($"/projects/{projectContract.ProjectId}/contracts/{projectContract.Id}/delegated-roles");
            var body = await delegatesResponse.Content.ReadAsStringAsync();

            if (!delegatesResponse.IsSuccessStatusCode)
            {
                log.LogWarning($"Error retrieving delegates for contract '{projectContract.Id}' in project '{projectContract.ProjectId}'");
                return new List<DelegatedRole>();
            }

            return JsonConvert.DeserializeObject<List<DelegatedRole>>(body);
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
