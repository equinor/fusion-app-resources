﻿using Fusion.ApiClients.Org;
using Fusion.Integration.Notification;
using Fusion.Integration.Org;
using Fusion.Resources.Api.Notifications.Markdown;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain;
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
    public class ExternalRequestsNotificationHandler :
        INotificationHandler<RequestDeclinedByCompany>,
        INotificationHandler<RequestDeclinedByContractor>
    {
        private readonly IMediator mediator;
        private readonly IFusionNotificationClient notificationClient;
        private readonly IProjectOrgResolver orgResolver;

        public ExternalRequestsNotificationHandler(IMediator mediator, IFusionNotificationClient notificationClient, IProjectOrgResolver orgResolver)
        {
            this.mediator = mediator;
            this.notificationClient = notificationClient;
            this.orgResolver = orgResolver;
        }

        public async Task Handle(RequestDeclinedByCompany notification, CancellationToken cancellationToken)
        {
            var request = await GetRequestAsync(notification.RequestId);
            var recipients = await CalculateExternalCRRecipientsAsync(request);

            //only notify creator if not CR and if not the person that declined request
            if (!recipients.Contains(request.CreatedBy.AzureUniqueId) && notification.DeclinedBy.AzureUniqueId != request.CreatedBy.AzureUniqueId)
                recipients.Add(request.CreatedBy.AzureUniqueId);

            foreach (var recipient in recipients)
            {
                await notificationClient.CreateNotificationForUserAsync(recipient, $"Request for {request.Position.Name} was declined", builder =>
                {
                    builder.AddDescription(NotificationDescription.RequestDeclinedByCompany(request, notification.Reason, notification.DeclinedBy));
                });
            }
        }

        public async Task Handle(RequestDeclinedByContractor notification, CancellationToken cancellationToken)
        {
            var request = await GetRequestAsync(notification.RequestId);

            //don't need to notify the person who created request if person also is decliner.
            if (request.CreatedBy.AzureUniqueId != notification.DeclinedBy.AzureUniqueId)
            {
                await notificationClient.CreateNotificationForUserAsync(request.CreatedBy.AzureUniqueId, $"Request for {request.Position.Name} was declined", builder =>
                {
                    builder.AddDescription(NotificationDescription.RequestDeclinedByExternal(request, notification.Reason, notification.DeclinedBy));
                });
            }
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
            var recipients = new List<Guid>();

            if (externalCompanyRep?.AssignedPerson?.AzureUniqueId != null && !recipients.Contains(externalCompanyRep.AssignedPerson.AzureUniqueId.Value))
                recipients.Add(externalCompanyRep.AssignedPerson.AzureUniqueId.Value);

            if (externalContractRep?.AssignedPerson?.AzureUniqueId != null && !recipients.Contains(externalContractRep.AssignedPerson.AzureUniqueId.Value))
                recipients.Add(externalContractRep.AssignedPerson.AzureUniqueId.Value);

            var distinctDelegates = externalDelegates.Where(d => !recipients.Contains(d.Person.AzureUniqueId))
                .Select(d => d.Person.AzureUniqueId)
                .Distinct();

            recipients.AddRange(distinctDelegates);

            return recipients;
        }

        private class NotificationDescription
        {
            public static string RequestCreatedAsync(QueryPersonnelRequest request, string? activeRequestsUrl) => new MarkdownDocument()
                .Paragraph($"Request was created by {request.CreatedBy?.Name} ({request.CreatedBy?.Mail}).")
                .Paragraph($"Please review and follow up request in Resources")
                .List(l => l
                    .ListItem($"{MdToken.Bold("Project:")} {request.Project?.Name}")
                    .ListItem($"{MdToken.Bold("Contract name:")} {request.Contract?.Name}")
                    .ListItem($"{MdToken.Bold("Contract number:")} {request.Contract?.ContractNumber}"))
                .LinkParagraph("Open Resources active requests", activeRequestsUrl)
                .Build();

            public static string RequestApprovedByExternal(QueryPersonnelRequest request, string? activeRequestsUrl, DbPerson approvedBy) => new MarkdownDocument()
                .Paragraph($"Request was approved by {approvedBy?.Name} ({approvedBy?.Mail}).")
                .Paragraph($"Please review and follow up request in Resources")
                .List(l => l
                    .ListItem($"{MdToken.Bold("Project:")} {request.Project?.Name}")
                    .ListItem($"{MdToken.Bold("Contract name:")} {request.Contract?.Name}")
                    .ListItem($"{MdToken.Bold("Contract number:")} {request.Contract?.ContractNumber}"))
                .LinkParagraph("Open Resources active requests", activeRequestsUrl)
                .Build();

            public static string RequestApprovedByCompany(QueryPersonnelRequest request, DbPerson approvedBy) => new MarkdownDocument()
                .Paragraph($"Request was approved by {approvedBy?.Name} ({approvedBy?.Mail}) and is now completed")
                .List(l => l
                    .ListItem($"{MdToken.Bold("Project:")} {request.Project?.Name}")
                    .ListItem($"{MdToken.Bold("Contract name:")} {request.Contract?.Name}")
                    .ListItem($"{MdToken.Bold("Contract number:")} {request.Contract?.ContractNumber}"))
                .Build();

            public static string RequestDeclinedByCompany(QueryPersonnelRequest request, string reason, DbPerson declinedBy) => new MarkdownDocument()
                .Paragraph($"Request was declined by {declinedBy?.Name} ({declinedBy?.Mail})")
                .Paragraph($"{MdToken.Bold("Reason")}: {reason}")
                .List(l => l
                    .ListItem($"{MdToken.Bold("Project:")} {request.Project?.Name}")
                    .ListItem($"{MdToken.Bold("Contract name:")} {request.Contract?.Name}")
                    .ListItem($"{MdToken.Bold("Contract number:")} {request.Contract?.ContractNumber}"))
                .Build();

            public static string RequestDeclinedByExternal(QueryPersonnelRequest request, string reason, DbPerson declinedBy) => new MarkdownDocument()
                .Paragraph($"Request was declined by {declinedBy?.Name} ({declinedBy?.Mail})")
                .Paragraph($"{MdToken.Bold("Reason")}: {reason}")
                .List(l => l
                    .ListItem($"{MdToken.Bold("Project:")} {request.Project?.Name}")
                    .ListItem($"{MdToken.Bold("Contract name:")} {request.Contract?.Name}")
                    .ListItem($"{MdToken.Bold("Contract number:")} {request.Contract?.ContractNumber}"))
                .Build();
        }
    }
}
