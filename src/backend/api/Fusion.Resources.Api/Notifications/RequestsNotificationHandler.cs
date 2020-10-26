using Fusion.ApiClients.Org;
using Fusion.Integration.Notification;
using Fusion.Integration.Org;
using Fusion.Resources.Api.Notifications.Markdown;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Notifications;
using Fusion.Resources.Domain.Notifications.Request;
using Fusion.Resources.Domain.Queries;
using MediatR;
using System;
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

        public RequestsNotificationHandler(IMediator mediator, IFusionNotificationClient notificationClient, IProjectOrgResolver orgResolver)
        {
            this.mediator = mediator;
            this.notificationClient = notificationClient;
            this.orgResolver = orgResolver;
        }

        public async Task Handle(RequestCreated notification, CancellationToken cancellationToken)
        {
            var request = await GetRequestAsync(notification.RequestId);

            if (request == null)
                return;

            var contract = await ResolveContractAsync(request);
            var extCompanyRep = contract.ExternalCompanyRep.GetActiveInstance();

            if (extCompanyRep != null && extCompanyRep.AssignedPerson != null)
            {
                await notificationClient.CreateNotificationAsync(notification => notification
                    .WithRecipient(extCompanyRep.AssignedPerson.Mail)
                    .WithDescriptionMarkdown(NotificationDescription.RequestCreated(request)));
            }

            var extContractRep = contract.ExternalContractRep.GetActiveInstance();

            if (extContractRep != null && extContractRep.AssignedPerson != null)
            {
                await notificationClient.CreateNotificationAsync(notification => notification
                    .WithRecipient(extContractRep.AssignedPerson.Mail)
                    .WithDescriptionMarkdown(NotificationDescription.RequestCreated(request)));
            }
        }

        public async Task Handle(RequestApprovedByCompany notification, CancellationToken cancellationToken)
        {
            var request = await GetRequestAsync(notification.RequestId);

            if (request == null)
                return;

            var contract = await ResolveContractAsync(request);
            var extCompanyRep = contract.ExternalCompanyRep.GetActiveInstance();

            if (extCompanyRep != null && extCompanyRep.AssignedPerson != null)
            {
                await notificationClient.CreateNotificationAsync(notification => notification
                    .WithRecipient(extCompanyRep.AssignedPerson.Mail)
                    .WithTitle("New request was approved by Equinor CR")
                    .WithDescriptionMarkdown(NotificationDescription.RequestApprovedByCompany(request)));
            }

            var extContractRep = contract.ExternalContractRep.GetActiveInstance();

            if (extContractRep != null && extContractRep.AssignedPerson != null)
            {
                await notificationClient.CreateNotificationAsync(notification => notification
                    .WithRecipient(extContractRep.AssignedPerson.Mail)
                    .WithTitle("New request was approved by Equinor CR")
                    .WithDescriptionMarkdown(NotificationDescription.RequestApprovedByCompany(request)));
            }

            //to creator of request
            await notificationClient.CreateNotificationAsync(notification => notification
                   .WithRecipient(request.CreatedBy.AzureUniqueId)
                   .WithTitle("New request was approved by Equinor CR")
                   .WithDescriptionMarkdown(NotificationDescription.RequestApprovedByCompany(request)));
        }

        public async Task Handle(RequestDeclinedByCompany notification, CancellationToken cancellationToken)
        {
            var request = await GetRequestAsync(notification.RequestId);

            if (request == null)
                return;

            var contract = await ResolveContractAsync(request);
            var extCompanyRep = contract.ExternalCompanyRep.GetActiveInstance();

            if (extCompanyRep != null && extCompanyRep.AssignedPerson != null)
            {
                await notificationClient.CreateNotificationAsync(n => n
                    .WithRecipient(extCompanyRep.AssignedPerson.Mail)
                    .WithTitle("Your request was declined by Equinor CR")
                    .WithDescriptionMarkdown(NotificationDescription.RequestDeclinedByCompany(request, notification.Reason)));
            }

            var extContractRep = contract.ExternalContractRep.GetActiveInstance();

            if (extContractRep != null && extContractRep.AssignedPerson != null)
            {
                await notificationClient.CreateNotificationAsync(n => n
                    .WithRecipient(extContractRep.AssignedPerson.Mail)
                    .WithTitle("Your request was declined by Equinor CR")
                    .WithDescriptionMarkdown(NotificationDescription.RequestDeclinedByCompany(request, notification.Reason)));
            }

            //to creator of request
            await notificationClient.CreateNotificationAsync(n => n
                   .WithRecipient(request.CreatedBy.AzureUniqueId)
                   .WithTitle("Your request was declined by Equinor CR")
                   .WithDescriptionMarkdown(NotificationDescription.RequestDeclinedByCompany(request, notification.Reason)));
        }

        public async Task Handle(RequestApprovedByContractor notification, CancellationToken cancellationToken)
        {
            var request = await GetRequestAsync(notification.RequestId);

            if (request == null)
                return;

            var contract = await ResolveContractAsync(request);
            var companyRep = contract.CompanyRep.GetActiveInstance();

            if (companyRep != null && companyRep.AssignedPerson != null)
            {
                await notificationClient.CreateNotificationAsync(n => n
                    .WithRecipient(companyRep.AssignedPerson.Mail)
                    .WithTitle("New request was approved by External CR")
                    .WithDescriptionMarkdown(NotificationDescription.RequestApprovedByExternal(request)));
            }

            var contractRep = contract.ContractRep.GetActiveInstance();

            if (contractRep != null && contractRep.AssignedPerson != null)
            {
                await notificationClient.CreateNotificationAsync(n => n
                    .WithRecipient(contractRep.AssignedPerson.Mail)
                    .WithTitle("New request was approved by External CR")
                    .WithDescriptionMarkdown(NotificationDescription.RequestApprovedByExternal(request)));
            }

            //to creator of request
            await notificationClient.CreateNotificationAsync(n => n
                .WithRecipient(request.CreatedBy.AzureUniqueId)
                .WithTitle("New request was approved by External CR")
                .WithDescriptionMarkdown(NotificationDescription.RequestApprovedByExternal(request)));
        }

        public async Task Handle(RequestDeclinedByContractor notification, CancellationToken cancellationToken)
        {
            var request = await GetRequestAsync(notification.RequestId);

            if (request == null)
                return;

            //to creator of request
            await notificationClient.CreateNotificationAsync(n => n
                .WithRecipient(request.CreatedBy.AzureUniqueId)
                .WithTitle("Your request was declined by External CR")
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

        private class NotificationDescription
        {
            public static string RequestCreated(QueryPersonnelRequest request) => new MarkdownDocument()
                .Paragraph($"New request was created by {request.CreatedBy?.Name} ({request.CreatedBy?.Mail})")
                .List(l => l
                    .ListItem($"{MdToken.Bold("Project:")} {request.Project?.Name}")
                    .ListItem($"{MdToken.Bold("Contract name:")} {request.Contract?.Name}")
                    .ListItem($"{MdToken.Bold("Contract number:")} {request.Contract?.ContractNumber}"))
                .Build();

            public static string RequestApprovedByExternal(QueryPersonnelRequest request) => new MarkdownDocument()
                .Paragraph($"New request was approved by External CR")
                .List(l => l
                    .ListItem($"{MdToken.Bold("Project:")} {request.Project?.Name}")
                    .ListItem($"{MdToken.Bold("Contract name:")} {request.Contract?.Name}")
                    .ListItem($"{MdToken.Bold("Contract number:")} {request.Contract?.ContractNumber}"))
                .Build();

            public static string RequestApprovedByCompany(QueryPersonnelRequest request) => new MarkdownDocument()
                .Paragraph($"New request was approved by Equinor CR")
                .List(l => l
                    .ListItem($"{MdToken.Bold("Project:")} {request.Project?.Name}")
                    .ListItem($"{MdToken.Bold("Contract name:")} {request.Contract?.Name}")
                    .ListItem($"{MdToken.Bold("Contract number:")} {request.Contract?.ContractNumber}"))
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
