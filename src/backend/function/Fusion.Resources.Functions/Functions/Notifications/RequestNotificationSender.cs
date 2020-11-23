using Fusion.Resources.Functions.ApiClients;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Fusion.Resources.Functions.Functions.Notifications
{
    public class RequestNotificationSender
    {
        private readonly IOrgApiClient orgApiClient;
        private readonly IResourcesApiClient resourcesApiClient;
        private readonly INotificationApiClient notificationApiClient;
        private readonly ISentNotificationsTableClient sentNotificationsClient;
        private readonly IUrlResolver urlResolver;
        private readonly IConfiguration configuration;
        private readonly TelemetryClient telemetryClient;
        private readonly ILogger<RequestNotificationSender> log;

        public RequestNotificationSender(IOrgApiClientFactory orgApiClientFactory,
            IResourcesApiClient resourcesApiClient,
            INotificationApiClient notificationApiClient,
            ISentNotificationsTableClient sentNotificationsClient,
            IUrlResolver urlResolver,
            ILoggerFactory loggerFactory,
            IConfiguration configuration,
            TelemetryClient telemetryClient)
        {
            orgApiClient = orgApiClientFactory.CreateClient(ApiClientMode.Application);
            this.resourcesApiClient = resourcesApiClient;
            this.notificationApiClient = notificationApiClient;
            this.sentNotificationsClient = sentNotificationsClient;
            this.urlResolver = urlResolver;
            this.configuration = configuration;
            this.telemetryClient = telemetryClient;
            log = loggerFactory.CreateLogger<RequestNotificationSender>();
        }

        public async Task ProcessNotificationsAsync()
        {
            var projectContracts = await resourcesApiClient.GetProjectContractsAsync();

            foreach (var projectContract in projectContracts)
            {
                var operation = telemetryClient.StartOperation<DependencyTelemetry>($"Process contract '{projectContract.Name}' ({projectContract.Id}) in project '{projectContract.ProjectName}' ({projectContract.ProjectId})");
                operation.Telemetry.Type = "ASYNC";

                log.LogInformation($"Proccessing contract '{projectContract.Name}' ({projectContract.Id}) in project '{projectContract.ProjectName}' ({projectContract.ProjectId})");

                try
                {
                    //notify external CR approvers for newly created requests.
                    var approvers = await CalculateExternalCRRecipientsAsync(projectContract);
                    var requestList = await resourcesApiClient.GetTodaysContractRequests(projectContract, IResourcesApiClient.RequestState.Created);

                    if (requestList?.Value?.Any() ?? false)
                        await NotifyApprovers(projectContract, approvers, requestList);

                    //notify external CR approvers for requests that were approved. Rejections are handled immediately.
                    requestList = await resourcesApiClient.GetTodaysContractRequests(projectContract, IResourcesApiClient.RequestState.ApprovedByCompany);
                    if (requestList?.Value?.Any() ?? false)
                        await NotifyRequestsCompleted(projectContract, approvers, requestList);

                    //notify equinor CR approvers for requests recently submitted to company.
                    approvers = await CalculateInternalCRRecipientsAsync(projectContract);
                    requestList = await resourcesApiClient.GetTodaysContractRequests(projectContract, IResourcesApiClient.RequestState.SubmittedToCompany);

                    if (requestList?.Value?.Any() ?? false)
                        await NotifyApprovers(projectContract, approvers, requestList);
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
        }

        private async Task NotifyApprovers(IResourcesApiClient.ProjectContract projectContract, List<Guid> approvers, IResourcesApiClient.PersonnelRequestList requestList)
        {
            foreach (var recipient in approvers)
            {
                var delay = await ResolveDelayAsync(recipient);
                var pendingRequests = await ResolvePendingRequests(requestList, recipient, delay);
                var notificationBody = await CreatePendingApprovalBodyAsync(projectContract, pendingRequests);

                if (pendingRequests.Any())
                {
                    var successfull = await notificationApiClient.PostNewNotificationAsync(recipient, $"Request(s) are pending your approval", notificationBody, INotificationApiClient.EmailPriority.High);

                    if (successfull)
                    {
                        //add to "sent" table when successfully notified
                        foreach (var request in pendingRequests)
                        {
                            await sentNotificationsClient.AddToSentNotifications(request.Id, recipient, request.State);
                        }
                    }
                }
            }
        }

        private async Task NotifyRequestsCompleted(IResourcesApiClient.ProjectContract projectContract, List<Guid> approvers, IResourcesApiClient.PersonnelRequestList requestList)
        {
            foreach (var recipient in approvers)
            {
                var delay = await ResolveDelayAsync(recipient);
                var pendingRequests = await ResolvePendingRequests(requestList, recipient, delay);
                var notificationBody = await CreateRequestsApprovedBodyAsync(projectContract, pendingRequests);

                if (pendingRequests.Any())
                {
                    var successfull = await notificationApiClient.PostNewNotificationAsync(recipient, $"Request(s) are approved", notificationBody, INotificationApiClient.EmailPriority.High);

                    if (successfull)
                    {
                        //add to "sent" table when successfully notified
                        foreach (var request in pendingRequests)
                        {
                            await sentNotificationsClient.AddToSentNotifications(request.Id, recipient, request.State);
                        }
                    }
                }
            }
        }

        private async Task<List<IResourcesApiClient.PersonnelRequest>> ResolvePendingRequests(IResourcesApiClient.PersonnelRequestList requestList, Guid recipient, int delay)
        {
            var pendingRequests = new List<IResourcesApiClient.PersonnelRequest>();

            foreach (var request in requestList?.Value)
            {
                if ((DateTime.UtcNow - request.LastActivity).TotalMinutes < delay)
                {
                    log.LogInformation($"Skipping request '{request.Id}' with lastActivity = '{request.LastActivity}'");
                    continue;
                }

                if (await sentNotificationsClient.NotificationWasSentAsync(request.Id, recipient, request.State))
                {
                    log.LogInformation($"Request '{request.Id}' was already notified for '{recipient}'");
                    continue;
                }

                pendingRequests.Add(request);
            }

            return pendingRequests;
        }

        private async Task<int> ResolveDelayAsync(Guid recipient)
        {
            var settings = await notificationApiClient.GetSettingsForUser(recipient);
            var minDelay = configuration.GetValue("RequestNotifications_min_delay", 60); //apply a minimum delay of 60 minutes if not configured
            var delay = Math.Max(settings.Delay, minDelay);

            log.LogInformation($"Current delay is '{delay}' mins");
            return delay;
        }

        private async Task<string> CreatePendingApprovalBodyAsync(IResourcesApiClient.ProjectContract projectContract, List<IResourcesApiClient.PersonnelRequest> requests)
        {
            var url = await urlResolver.ResolveActiveRequestsAsync(projectContract);

            var notificationBody = new MarkdownDocument()
            .LinkParagraph($"Please review and follow up in Resources", url)
            .List(l => l
                .ListItem($"{MdToken.Bold("Project:")} {projectContract.ProjectName}")
                .ListItem($"{MdToken.Bold("Contract name:")} {projectContract.Name}")
                .ListItem($"{MdToken.Bold("Contract number:")} {projectContract.ContractNumber}"))
            .Paragraph(MdToken.Newline())
            .Paragraph("Pending requests:")
            .List(list =>
            {
                foreach (var request in requests)
                {
                    //{person} as {postitle} from {fromdate} to {toDate}
                    list.ListItem($"{MdToken.Bold($"{request.Person.Name} ({request.Person.Mail})")} as {MdToken.Bold($"{request.Position.Name}")}");
                }
            })
            .Build();
            return notificationBody;
        }

        private async Task<string> CreateRequestsApprovedBodyAsync(IResourcesApiClient.ProjectContract projectContract, List<IResourcesApiClient.PersonnelRequest> requests)
        {
            var url = await urlResolver.ResolveActiveRequestsAsync(projectContract);

            var notificationBody = new MarkdownDocument()
            .Paragraph("Request(s) were approved and are now completed")
            .List(l => l
                .ListItem($"{MdToken.Bold("Project:")} {projectContract.ProjectName}")
                .ListItem($"{MdToken.Bold("Contract name:")} {projectContract.Name}")
                .ListItem($"{MdToken.Bold("Contract number:")} {projectContract.ContractNumber}"))
            .Paragraph(MdToken.Newline())
            .Paragraph("Requests:")
            .List(list =>
            {
                foreach (var request in requests)
                {
                    //{person} as {postitle} from {fromdate} to {toDate}
                    list.ListItem($"{MdToken.Bold($"{request.Person.Name} ({request.Person.Mail})")} as {MdToken.Bold($"{request.Position.Name}")}");
                }
            })
            .Build();
            return notificationBody;
        }

        private async Task<List<Guid>> CalculateInternalCRRecipientsAsync(IResourcesApiClient.ProjectContract projectContract)
        {
            var recipients = new List<Guid>();
            var orgContract = await orgApiClient.GetContractV2Async(projectContract.ProjectId, projectContract.Id);
            var internalCompanyRep = orgContract?.CompanyRep?.Instances?.FirstOrDefault(i => i.AppliesFrom <= DateTime.Today && i.AppliesTo >= DateTime.Today);
            var internalContractRep = orgContract?.ContractRep?.Instances?.FirstOrDefault(i => i.AppliesFrom <= DateTime.Today && i.AppliesTo >= DateTime.Today);

            if (internalCompanyRep?.AssignedPerson?.AzureUniqueId != null)
                recipients.Add(internalCompanyRep.AssignedPerson.AzureUniqueId.Value);

            if (internalContractRep?.AssignedPerson?.AzureUniqueId != null && !recipients.Contains(internalContractRep.AssignedPerson.AzureUniqueId.Value))
                recipients.Add(internalContractRep.AssignedPerson.AzureUniqueId.Value);

            var delegates = await resourcesApiClient.RetrieveDelegatesForContractAsync(projectContract);
            var distinctDelegates = delegates?
                .Where(d => d.Person?.AzureUniquePersonId != null && d.Classification == "Internal" && !recipients.Contains(d.Person.AzureUniquePersonId.Value))
                .Select(d => d.Person.AzureUniquePersonId.Value)
                .Distinct();

            if (distinctDelegates != null)
                recipients.AddRange(distinctDelegates);

            log.LogInformation($"Calculated the following internal CR recipients: [{string.Join(",", recipients)}]");

            return recipients;
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

            log.LogInformation($"Calculated the following external CR recipients: [{string.Join(",", recipients)}]");

            return recipients;
        }
    }
}

