using Fusion.ApiClients.Org;
using Fusion.Integration.Notification;
using Fusion.Integration.Org;
using Fusion.Resources.Api.Notifications.Markdown;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Notifications;
using Fusion.Resources.Domain.Notifications.Request;
using Fusion.Resources.Domain.Queries;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Notifications
{
    public class RequestsNotificationHandler :
        INotificationHandler<RequestCreated>,
        INotificationHandler<RequestApprovedByCompany>,
        INotificationHandler<RequestApprovedByContractor>,
        INotificationHandler<RequestDeclinedByCompany>,
        INotificationHandler<RequestDeclinedByContractor>
    {
        private readonly IMediator mediator;
        private readonly IFusionNotificationClient notificationClient;
        private readonly IProjectOrgResolver orgResolver;
        private readonly IUrlResolver urlResolver;

        public RequestsNotificationHandler(IMediator mediator, IFusionNotificationClient notificationClient, IProjectOrgResolver orgResolver, IUrlResolver urlResolver)
        {
            this.mediator = mediator;
            this.notificationClient = notificationClient;
            this.orgResolver = orgResolver;
            this.urlResolver = urlResolver;
        }

        public async Task Handle(RequestCreated notification, CancellationToken cancellationToken)
        {
            var request = await GetRequestAsync(notification.RequestId);

            if (request == null)
                return;

            var requestsUrl = await urlResolver.ResolveActiveRequests(request.Project.OrgProjectId, request.Contract.OrgContractId);
            var recipients = await CalculateExternalCRRecipientsAsync(request);

            foreach (var recipient in recipients)
            {
                await notificationClient.CreateNotificationAsync(notification => notification
                    .WithRecipient(recipient)
                    .WithTitle($"Request for {request.Position.Name} was created")
                    .WithDescriptionMarkdown(NotificationDescription.RequestCreatedAsync(request, requestsUrl)));
            }
        }

        public async Task Handle(RequestApprovedByCompany notification, CancellationToken cancellationToken)
        {
            var request = await GetRequestAsync(notification.RequestId);

            if (request == null)
                return;

            var requestsUrl = await urlResolver.ResolveActiveRequests(request.Project.OrgProjectId, request.Contract.OrgContractId);
            var recipients = await CalculateExternalCRRecipientsAsync(request);
            recipients.Add(request.CreatedBy.AzureUniqueId);

            foreach (var recipient in recipients)
            {
                await notificationClient.CreateNotificationAsync(notification => notification
                   .WithRecipient(recipient)
                   .WithTitle($"Request for {request.Position.Name} was approved by Equinor CR")
                   .WithDescriptionMarkdown(NotificationDescription.RequestApprovedByCompany(request, requestsUrl)));
            }
        }

        public async Task Handle(RequestDeclinedByCompany notification, CancellationToken cancellationToken)
        {
            var request = await GetRequestAsync(notification.RequestId);

            if (request == null)
                return;

            var requestsUrl = await urlResolver.ResolveActiveRequests(request.Project.OrgProjectId, request.Contract.OrgContractId);
            var recipients = await CalculateExternalCRRecipientsAsync(request);
            recipients.Add(request.CreatedBy.AzureUniqueId);

            foreach (var recipient in recipients)
            {
                await notificationClient.CreateNotificationAsync(n => n
                   .WithRecipient(recipient)
                   .WithTitle($"Request for {request.Position.Name} was declined by Equinor CR")
                   .WithDescriptionMarkdown(NotificationDescription.RequestDeclinedByCompany(request, notification.Reason)));
            }
        }

        public async Task Handle(RequestApprovedByContractor notification, CancellationToken cancellationToken)
        {
            var request = await GetRequestAsync(notification.RequestId);

            if (request == null)
                return;

            var requestsUrl = await urlResolver.ResolveActiveRequests(request.Project.OrgProjectId, request.Contract.OrgContractId);
            var recipients = await CalculateInternalCRRecipientsAsync(request);
            recipients.Add(request.CreatedBy.AzureUniqueId);

            foreach (var recipient in recipients)
            {
                await notificationClient.CreateNotificationAsync(n => n
                    .WithRecipient(recipient)
                    .WithTitle($"Request for {request.Position.Name} was approved by External CR")
                    .WithDescriptionMarkdown(NotificationDescription.RequestApprovedByExternal(request, requestsUrl)));
            }
        }

        public async Task Handle(RequestDeclinedByContractor notification, CancellationToken cancellationToken)
        {
            var request = await GetRequestAsync(notification.RequestId);

            if (request == null)
                return;

            //to creator of request
            await notificationClient.CreateNotificationAsync(n => n
                .WithRecipient(request.CreatedBy.AzureUniqueId)
                .WithTitle($"Request for {request.Position.Name} was declined by External CR")
                .WithDescriptionMarkdown(NotificationDescription.RequestDeclinedByExternal(request, notification.Reason)));
        }

        private async Task<QueryPersonnelRequest> GetRequestAsync(Guid requestId)
        {
            var query = new GetContractPersonnelRequest(requestId);
            var request = await mediator.Send(query);

            return request;
        }

        private async Task<ApiProjectContractV2> ResolveContractAsync(QueryPersonnelRequest request)
        {
            var resolvedContract = await orgResolver.ResolveContractAsync(request.Project.OrgProjectId, request.Contract.OrgContractId);

            if (resolvedContract == null)
                throw new InvalidOperationException($"Cannot resolve contract for request {request.Id}");

            return resolvedContract;
        }

        private async Task<List<Guid>> CalculateExternalCRRecipientsAsync(QueryPersonnelRequest request)
        {
            var contract = await orgResolver.ResolveContractAsync(request.Project.OrgProjectId, request.Contract.OrgContractId);

            if (contract == null)
                throw new InvalidOperationException($"Cannot resolve contract for request {request.Id}");

            var externalCompanyRep = contract.ExternalCompanyRep.GetActiveInstance();
            var externalContractRep = contract.ExternalContractRep.GetActiveInstance();
            var delegates = await mediator.Send(GetContractDelegatedRoles.ForContract(request.Project.OrgProjectId, contract.Id));
            var externalDelegates = delegates.Where(o => o.Classification == DbDelegatedRoleClassification.External).ToList();
            var requestsUrl = await urlResolver.ResolveActiveRequests(request.Project.OrgProjectId, request.Contract.OrgContractId);

            var recipients = new List<Guid>();

            if (externalCompanyRep?.AssignedPerson?.AzureUniqueId != null && !recipients.Contains(externalCompanyRep.AssignedPerson.AzureUniqueId.Value))
                recipients.Add(externalCompanyRep.AssignedPerson.AzureUniqueId.Value);

            if (externalContractRep?.AssignedPerson?.AzureUniqueId != null && !recipients.Contains(externalContractRep.AssignedPerson.AzureUniqueId.Value))
                recipients.Add(externalContractRep.AssignedPerson.AzureUniqueId.Value);

            recipients.AddRange(externalDelegates.Where(d => !recipients.Contains(d.Person.AzureUniqueId)).Select(d => d.Person.AzureUniqueId));

            return recipients;
        }

        private async Task<List<Guid>> CalculateInternalCRRecipientsAsync(QueryPersonnelRequest request)
        {
            var contract = await ResolveContractAsync(request);
            var companyRep = contract.CompanyRep.GetActiveInstance();
            var contractRep = contract.ContractRep.GetActiveInstance();
            var delegates = await mediator.Send(GetContractDelegatedRoles.ForContract(request.Project.OrgProjectId, contract.Id));
            var internalDelegates = delegates.Where(o => o.Classification == DbDelegatedRoleClassification.Internal).ToList();
            var requestsUrl = await urlResolver.ResolveActiveRequests(request.Project.OrgProjectId, request.Contract.OrgContractId);

            var recipients = new List<Guid>()
            {
                request.CreatedBy.AzureUniqueId
            };

            if (companyRep?.AssignedPerson?.AzureUniqueId != null && !recipients.Contains(companyRep.AssignedPerson.AzureUniqueId.Value))
                recipients.Add(companyRep.AssignedPerson.AzureUniqueId.Value);

            if (contractRep?.AssignedPerson?.AzureUniqueId != null && !recipients.Contains(contractRep.AssignedPerson.AzureUniqueId.Value))
                recipients.Add(contractRep.AssignedPerson.AzureUniqueId.Value);

            recipients.AddRange(internalDelegates.Where(d => !recipients.Contains(d.Person.AzureUniqueId)).Select(d => d.Person.AzureUniqueId));

            return recipients;
        }

        private class NotificationDescription
        {
            public static string RequestCreatedAsync(QueryPersonnelRequest request, string? activeRequestsUrl) => new MarkdownDocument()
                    .Paragraph($"New request was created by {request.CreatedBy?.Name} ({request.CreatedBy?.Mail})")
                    .List(l => l
                        .ListItem($"{MdToken.Bold("Project:")} {request.Project?.Name}")
                        .ListItem($"{MdToken.Bold("Contract name:")} {request.Contract?.Name}")
                        .ListItem($"{MdToken.Bold("Contract number:")} {request.Contract?.ContractNumber}"))
                    .LinkParagraph("Open active requests", activeRequestsUrl)
                    .Build();

            public static string RequestApprovedByExternal(QueryPersonnelRequest request, string? activeRequestsUrl) => new MarkdownDocument()
                .Paragraph($"New request was approved by External CR")
                .List(l => l
                    .ListItem($"{MdToken.Bold("Project:")} {request.Project?.Name}")
                    .ListItem($"{MdToken.Bold("Contract name:")} {request.Contract?.Name}")
                    .ListItem($"{MdToken.Bold("Contract number:")} {request.Contract?.ContractNumber}"))
                .LinkParagraph("Open active requests", activeRequestsUrl)
                .Build();

            public static string RequestApprovedByCompany(QueryPersonnelRequest request, string? activeRequestsUrl) => new MarkdownDocument()
                .Paragraph($"New request was approved by Equinor CR")
                .List(l => l
                    .ListItem($"{MdToken.Bold("Project:")} {request.Project?.Name}")
                    .ListItem($"{MdToken.Bold("Contract name:")} {request.Contract?.Name}")
                    .ListItem($"{MdToken.Bold("Contract number:")} {request.Contract?.ContractNumber}"))
                .LinkParagraph("Open active requests", activeRequestsUrl)
                .Build();

            public static string RequestDeclinedByCompany(QueryPersonnelRequest request, string reason) => new MarkdownDocument()
                .Paragraph($"Your request was declined by Equinor CR. Reason: {reason}")
                .List(l => l
                    .ListItem($"{MdToken.Bold("Project:")} {request.Project?.Name}")
                    .ListItem($"{MdToken.Bold("Contract name:")} {request.Contract?.Name}")
                    .ListItem($"{MdToken.Bold("Contract number:")} {request.Contract?.ContractNumber}"))
                .Build();

            public static string RequestDeclinedByExternal(QueryPersonnelRequest request, string reason) => new MarkdownDocument()
                .Paragraph($"Your request was declined by External CR. Reason: {reason}")
                .List(l => l
                    .ListItem($"{MdToken.Bold("Project:")} {request.Project?.Name}")
                    .ListItem($"{MdToken.Bold("Contract name:")} {request.Contract?.Name}")
                    .ListItem($"{MdToken.Bold("Contract number:")} {request.Contract?.ContractNumber}"))
                .Build();
        }
    }
}
