using Fusion.Resources.Functions.Functions.Notifications;
using Fusion.Resources.Functions.TableStorage;
using Microsoft.Azure.Cosmos.Table;
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
        private readonly IOrgApiClient orgClient;
        private readonly TableStorageClient tableStorageClient;

        public RequestSummaryNotification(IHttpClientFactory httpClientFactory, IOrgApiClientFactory orgApiClientFactory, TableStorageClient tableStorageClient)
        {
            resourcesClient = httpClientFactory.CreateClient(HttpClientNames.Application.Resources);
            notificationsClient = httpClientFactory.CreateClient(HttpClientNames.Application.Notifications);
            orgClient = orgApiClientFactory.CreateClient(ApiClientMode.Application);
            this.tableStorageClient = tableStorageClient;
        }

        [FunctionName("request-summary-notification")]
        public async Task SendRequestSummaryNotification([TimerTrigger("*/5 * * * *", RunOnStartup = true)] TimerInfo timer, ILogger log)
        {
            log.LogInformation("Request notification summary starting");

            var projectContracts = await GetProjectContractsAsync(log);

            foreach (var projectContract in projectContracts)
            {
                var approvers = await CalculateExternalCRRecipientsAsync(projectContract, log);

                //TODO: Re-instate lastActivity filter before finalizing PR
                //var odataFilter = $"$filter=lastActivity gt {DateTime.Today} and state eq 'SubmittedToCompany'";
                var odataFilter = $"$filter=state eq 'SubmittedToCompany'";
                var requestsResponse = await resourcesClient.GetAsync($"projects/{projectContract.ProjectId}/contracts/{projectContract.Id}/resources/requests?{odataFilter}");

                var body = await requestsResponse.Content.ReadAsStringAsync();
                var requestList = JsonConvert.DeserializeObject<PersonnelRequestList>(body);

                foreach (var recipient in approvers)
                {
                    bool hasPendingNotification = false; //identifies whether the user has at least one request to approve. Otherwise, no notification.

                    var notificationBody = new MarkdownDocument()
                    .Paragraph($"Please review and follow up request in Resources")
                    .List(l => l
                        .ListItem($"{MdToken.Bold("Project:")} {projectContract.ProjectName}")
                        .ListItem($"{MdToken.Bold("Contract name:")} {projectContract.Name}")
                        .ListItem($"{MdToken.Bold("Contract number:")} {projectContract.Number}"))
                    .LinkParagraph("Open Resources active requests", "InsertUrlHere")
                    .Paragraph(MdToken.Newline())
                    .Paragraph("These are the pending requests:")
                    .List(async list =>
                    {
                        foreach (var request in requestList.Value)
                        {
                            //check if particular request was notified already using table storage
                            var table = await tableStorageClient.GetTableAsync("ResourcesSentNotifications");
                            var result = await table.GetByKeysAsync<SentNotifications>(request.Id.ToString(), recipient.ToString());

                            if (result != null)
                            {
                                log.LogInformation($"Request '{request.Id}' was already notified for '{recipient}' at {result.Timestamp:s}");
                                continue;
                            }

                            hasPendingNotification = true;

                            //{person} as {postitle} from {fromdate} to {toDate}
                            list.ListItem($"{MdToken.Bold($"{request.Person.Name} ({request.Person.Mail})")} " +
                                $"as {MdToken.Bold($"{request.Position.Name}")} " +
                                $"from {request.Position.AppliesFrom} to {request.Position.AppliesTo}");

                            var operation = TableOperation.InsertOrReplace(new SentNotifications { PartitionKey = request.Id.ToString(), RowKey = recipient.ToString() });
                            await table.ExecuteAsync(operation);
                        }
                    })
                    .Build();

                    if (hasPendingNotification)
                    {
                        var notification = new
                        {
                            AppKey = "resources",
                            Priority = "Default",
                            Title = $"Request(s) are pending your approval",
                            Description = notificationBody
                        };

                        var response = await notificationsClient.PostAsJsonAsync($"persons/{recipient}/notifications", notification);

                        if (!response.IsSuccessStatusCode)
                            log.LogInformation($"Failed to notify recipient with Azure ID '{recipient}': [{response.StatusCode}]");


                    }
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
            var recipients = new List<Guid>();
            //403 Forbidden when getting contract information - why?
            var orgContract = await orgClient.GetContractV2Async(projectContract.ProjectId, projectContract.Id);
            var externalCompanyRep = orgContract?.ExternalCompanyRep?.Instances?.FirstOrDefault(i => i.AppliesFrom <= DateTime.Today && i.AppliesTo >= DateTime.Today);
            var externalContractRep = orgContract?.ExternalContractRep?.Instances?.FirstOrDefault(i => i.AppliesFrom <= DateTime.Today && i.AppliesTo >= DateTime.Today);

            if (externalCompanyRep?.AssignedPerson?.AzureUniqueId != null)
                recipients.Add(externalCompanyRep.AssignedPerson.AzureUniqueId.Value);

            if (externalContractRep?.AssignedPerson?.AzureUniqueId != null && !recipients.Contains(externalContractRep.AssignedPerson.AzureUniqueId.Value))
                recipients.Add(externalContractRep.AssignedPerson.AzureUniqueId.Value);

            var delegates = await RetrieveDelegatsForContractAsync(projectContract, log);
            var distinctDelegates = delegates
                .Where(d => d.Person?.AzureUniquePersonId != null && d.Classification == "External" && !recipients.Contains(d.Person.AzureUniquePersonId.Value))
                .Select(d => d.Person.AzureUniquePersonId.Value)
                .Distinct();

            recipients.AddRange(distinctDelegates);

            log.LogInformation($"Calculated the following recipients: [{string.Join(",", recipients)}]");

            return recipients;
        }

        //private async Task<Guid> GetPersonForPosition(ProjectContract projectContract, Guid positionId, bool isContractPosition = false)
        //{
        //    var resourcePath = isContractPosition ? $"projects/{projectContract.ProjectId}/contracts/{projectContract.Id}/positions/{positionId}" :
        //        $"projects/{projectContract.ProjectId}/positions/{positionId}";

        //    var positionResponse = await orgClient.GetAsync(resourcePath);
        //    var body = await positionResponse.Content.ReadAsStringAsync();

        //    var response = JsonConvert.DeserializeAnonymousType(body,
        //        new
        //        {
        //            Instances = new[]
        //            {
        //                new
        //                {
        //                    AssignedPerson = new { AzureUniqueId = Guid.Empty }
        //                }
        //            }
        //        });

        //}

        private async Task<List<ProjectContract>> GetProjectContractsAsync(ILogger log)
        {
            var projectResponse = await resourcesClient.GetAsync($"projects");
            var body = await projectResponse.Content.ReadAsStringAsync();

            if (!projectResponse.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to retrieve projects from Resources API [{projectResponse.StatusCode}]. Body: {body}");
            }

            var projectList = JsonConvert.DeserializeAnonymousType(body, new[] { new { Id = Guid.Empty, OrgProjectId = Guid.Empty, Name = string.Empty } });
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
                contractList.value.ForEach(c =>
                {
                    c.ProjectId = project.OrgProjectId;
                    c.ProjectName = project.Name;
                });

                projectContracts.AddRange(contractList.value);
            }

            return projectContracts;
        }

        private class ProjectContract
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public string Number { get; set; }

            public Guid ProjectId { get; set; }
            public string ProjectName { get; set; }

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

            public RequestPosition Position { get; set; }

            public RequestPersonnel Person { get; set; }

            public class RequestPosition
            {
                public string Name { get; set; }
                public DateTime AppliesFrom { get; set; }
                public DateTime AppliesTo { get; set; }
            }

            public class RequestPersonnel
            {
                public string Name { get; set; }
                public string Mail { get; set; }
            }
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
