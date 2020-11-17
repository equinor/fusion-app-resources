using Fusion.Resources.Functions.ApiClients;
using Fusion.Resources.Functions.TableStorage;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.Functions.Notifications
{
    public class RequestNotificationSender
    {
        private readonly IOrgApiClient orgApiClient;
        private readonly IResourcesApiClient resourcesApiClient;
        private readonly INotificationApiClient notificationApiClient;
        private readonly ISentNotificationsTableClient sentNotificationsClient;
        private readonly ILogger<RequestNotificationSender> log;

        public RequestNotificationSender(IOrgApiClientFactory orgApiClientFactory,
            IResourcesApiClient resourcesApiClient,
            INotificationApiClient notificationApiClient,
            ISentNotificationsTableClient sentNotificationsClient,
            ILoggerFactory loggerFactory)
        {
            orgApiClient = orgApiClientFactory.CreateClient(ApiClientMode.Application);
            this.resourcesApiClient = resourcesApiClient;
            this.notificationApiClient = notificationApiClient;
            this.sentNotificationsClient = sentNotificationsClient;
            log = loggerFactory.CreateLogger<RequestNotificationSender>();
        }

        public async Task ProcessNotificationsAsync()
        {
            var projectContracts = await resourcesApiClient.GetProjectContractsAsync();

            foreach (var projectContract in projectContracts)
            {
                var approvers = await CalculateExternalCRRecipientsAsync(projectContract);

                foreach (var recipient in approvers)
                {
                    var delay = await notificationApiClient.GetDelayForUserAsync(recipient);

                    if (delay is null)
                    {
                        log.LogWarning($"Skipping notification");
                        continue;
                    }

                    log.LogInformation($"Current delay is '{delay}' mins");

                    var pendingRequests = new List<IResourcesApiClient.PersonnelRequest>();
                    var requestList = await resourcesApiClient.GetTodaysContractRequests(projectContract, "Created");

                    foreach (var request in requestList.Value)
                    {
                        if ((DateTime.UtcNow - request.LastActivity).TotalMinutes < delay)
                        {
                            log.LogInformation($"Skipping request '{request.Id}' with lastActivity = '{request.LastActivity}'");
                            continue;
                        }

                        if (await sentNotificationsClient.NotificationWasSentAsync(request.Id, recipient))
                        {
                            log.LogInformation($"Request '{request.Id}' was already notified for '{recipient}'");
                            continue;
                        }

                        pendingRequests.Add(request);
                    }

                    var notificationBody = new MarkdownDocument()
                    .Paragraph($"Please review and follow up request in Resources")
                    .List(l => l
                        .ListItem($"{MdToken.Bold("Project:")} {projectContract.ProjectName}")
                        .ListItem($"{MdToken.Bold("Contract name:")} {projectContract.Name}")
                        .ListItem($"{MdToken.Bold("Contract number:")} {projectContract.ContractNumber}"))
                    .LinkParagraph("Open Resources active requests", "InsertUrlHere")
                    .Paragraph(MdToken.Newline())
                    .Paragraph("These are the pending requests:")
                    .List(list =>
                    {
                        foreach (var request in pendingRequests)
                        {
                            //{person} as {postitle} from {fromdate} to {toDate}
                            list.ListItem($"{MdToken.Bold($"{request.Person.Name} ({request.Person.Mail})")} " +
                                $"as {MdToken.Bold($"{request.Position.Name}")} " +
                                $"from {request.Position.AppliesFrom} to {request.Position.AppliesTo}");
                        }
                    })
                    .Build();

                    if (pendingRequests.Any())
                    {
                        var successfull = await notificationApiClient.PostNewNotificationAsync(recipient, $"Request(s) are pending your approval", notificationBody);

                        //add to "sent" table when successfully notified
                        if (successfull)
                        {
                            foreach (var request in pendingRequests)
                            {
                                await sentNotificationsClient.AddToSentNotifications(request.Id, recipient);
                            }
                        }
                    }
                }
            }
        }

        private async Task<List<Guid>> CalculateExternalCRRecipientsAsync(IResourcesApiClient.ProjectContract projectContract)
        {
            var recipients = new List<Guid>();
            var orgContract = await orgApiClient.GetContractV2Async(projectContract.ProjectId, projectContract.Id);
            var externalCompanyRep = orgContract?.ExternalCompanyRep?.Instances?.FirstOrDefault(i => i.AppliesFrom <= DateTime.Today && i.AppliesTo >= DateTime.Today);
            var externalContractRep = orgContract?.ExternalContractRep?.Instances?.FirstOrDefault(i => i.AppliesFrom <= DateTime.Today && i.AppliesTo >= DateTime.Today);

            if (externalCompanyRep?.AssignedPerson?.AzureUniqueId != null)
                recipients.Add(externalCompanyRep.AssignedPerson.AzureUniqueId.Value);

            if (externalContractRep?.AssignedPerson?.AzureUniqueId != null && !recipients.Contains(externalContractRep.AssignedPerson.AzureUniqueId.Value))
                recipients.Add(externalContractRep.AssignedPerson.AzureUniqueId.Value);

            var delegates = await resourcesApiClient.RetrieveDelegatesForContractAsync(projectContract);
            var distinctDelegates = delegates?
                .Where(d => d.Person?.AzureUniquePersonId != null && d.Classification == "External" && !recipients.Contains(d.Person.AzureUniquePersonId.Value))
                .Select(d => d.Person.AzureUniquePersonId.Value)
                .Distinct();

            if (distinctDelegates != null)
                recipients.AddRange(distinctDelegates);

            log.LogInformation($"Calculated the following recipients: [{string.Join(",", recipients)}]");

            return recipients;
        }

    }
}

