using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.Functions
{
    public class RequestSummaryNotification
    {
        private readonly HttpClient resourcesClient;
        private readonly HttpClient notificationsClient;
        private readonly HttpClient orgClient;

        public RequestSummaryNotification(IHttpClientFactory httpClientFactory)
        {
            resourcesClient = httpClientFactory.CreateClient(HttpClientNames.Application.Resources);
            notificationsClient = httpClientFactory.CreateClient(HttpClientNames.Application.Notifications);
            orgClient = httpClientFactory.CreateClient(HttpClientNames.Application.Org);
        }

        [FunctionName("request-summary-notification")]
        public async Task SendRequestSummaryNotification([TimerTrigger("*/5 * * * *", RunOnStartup = true)] TimerInfo timer, ILogger log)
        {
            log.LogInformation("Request notification summary starting");

            var projectContracts = await GetProjectContractsAsync(log);

            foreach (var projectContract in projectContracts)
            {
                var delegates = await RetrieveDelegatsForContractAsync(log, projectContract);


                //TODO: Re-instate lastActivity filter before finalizing PR
                //var odataFilter = $"$filter=lastActivity gt {DateTime.Today} and state eq 'SubmittedToCompany'";
                var odataFilter = $"$filter=state eq 'SubmittedToCompany'";
                var requestsResponse = await resourcesClient.GetAsync($"projects/{projectContract.ProjectId}/contracts/{projectContract.Id}/resources/requests?{odataFilter}");

                var body = await requestsResponse.Content.ReadAsStringAsync();
                var requestList = JsonConvert.DeserializeObject<PersonnelRequestList>(body);

                //get company rep persons and delegates

                foreach (var request in requestList.Value)
                {
                    var id = request.Id;
                }
            }

            //get requests with latest activity since today which is ready for company approval
            //get distinct approvers
            //for each approver
            //check email delay preferences for user. Use that delay.
            //if now - lastActivity > delay
            //include in notification summary
            //end if
            //send notification to user
            //check sent notifications
            //add notifications to sent notifications

            //next user

            //for requests ready for external approval
            //group by approver
            //check email delay preferences for user. Use that delay or min. 60 minutes.
            //get requests with last activity for the last <delay> minutes, in state
            //check when approvers were notified last for this request
            //if not notified, send notification
        }

        private async Task<List<DelegatedRole>> RetrieveDelegatsForContractAsync(ProjectContract projectContract, ILogger log)
        {
            var delegatesResponse = await resourcesClient.GetAsync($"/projects/{projectContract.ProjectId}/contracts/{projectContract.Id}/delegated-roles");
            var body = await delegatesResponse.Content.ReadAsStringAsync();

            if (!delegatesResponse.IsSuccessStatusCode)
            {
                log.LogWarning($"Error retrieving delegates for contract '{projectContract.Id}' in project '{projectContract.ProjectId}'" +
                    $"No notifications will be sent to these users");

                return new List<DelegatedRole>();
            }
            else
            {
                return JsonConvert.DeserializeObject<List<DelegatedRole>>(body);
            }
        }

        private async Task<List<Guid>> CalculateExternalCRRecipientsAsync(ProjectContract projectContract, ILogger log)
        {
            var delegates = await RetrieveDelegatsForContractAsync(projectContract, log);


        }

        private async Task<Guid> GetPersonForPosition(ProjectContract projectContract, Guid positionId, bool isContractPosition = false)
        {
            var resourcePath = isContractPosition ? $"projects/{projectContract.ProjectId}/contracts/{projectContract.Id}/positions/{positionId}" : 
                $"projects/{projectContract.ProjectId}/positions/{positionId}";

            var positionResponse = await orgClient.GetAsync(resourcePath);
            var body = await positionResponse.Content.ReadAsStringAsync();

            var response = JsonConvert.DeserializeAnonymousType(body, new { instances })

        }

        private async Task<List<ProjectContract>> GetProjectContractsAsync(ILogger log)
        {
            var projectResponse = await resourcesClient.GetAsync($"projects");
            var body = await projectResponse.Content.ReadAsStringAsync();

            if (!projectResponse.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to retrieve projects from Resources API [{projectResponse.StatusCode}]. Body: {body}");
            }

            var projectList = JsonConvert.DeserializeAnonymousType(body, new[] { new { Id = Guid.Empty, OrgProjectId = Guid.Empty } });
            var projectContracts = new List<ProjectContract>();

            foreach (var project in projectList)
            {
                var contractResponse = await resourcesClient.GetAsync($"projects/{project.OrgProjectId}/contracts");
                body = await contractResponse.Content.ReadAsStringAsync();

                if (!contractResponse.IsSuccessStatusCode)
                {
                    log.LogWarning($"Failed to retrieve contracts for project '{project.OrgProjectId}' from Resources API [{projectResponse.StatusCode}]. Body: {body}. " +
                        $"Skipping notifications for this project.");
                    continue;
                }

                var contractList = JsonConvert.DeserializeAnonymousType(body, new { value = new List<ProjectContract>() });
                contractList.value.ForEach(c => c.ProjectId = project.OrgProjectId);
                projectContracts.AddRange(contractList.value);
            }

            return projectContracts;
        }

        private class ProjectContract
        {
            public Guid Id { get; set; }

            public Guid ProjectId { get; set; }

            public Guid? ContractResponsiblePositionId { get; set; }
            public Guid? CompanyRepPositionId { get; set; }
            public Guid? ExternalContractResponsiblePositionId { get; set; }
            public Guid? ExternalCompanyRepPositionId { get; set; }
        }

        private class PersonnelRequestList
        {
            public List<PersonnelRequest> Value { get; set; }
        }

        private class PersonnelRequest
        {
            public Guid Id { get; set; }

            public string State { get; set; }

            public string LastActivity { get; set; }
        }

        private class DelegatedRole
        {
            public string Classification { get; set; }

            public Person Person { get; set; }
        }

        private class Person
        {
            public Guid? AzureUniquePersonId { get; set; }

            public string Mail { get; set; }
        }
    }
}
