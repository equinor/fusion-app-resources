using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
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

        public async Task<List<ProjectContract>> GetProjectContractsAsync()
        {
            var projectResponse = await resourcesClient.GetAsync($"projects");
            var body = await projectResponse.Content.ReadAsStringAsync();

            if (!projectResponse.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to retrieve projects from Resources API [{projectResponse.StatusCode}]. Body: {body.Substring(0, 500)}"); //don't display all if body is very large
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
    }
}
