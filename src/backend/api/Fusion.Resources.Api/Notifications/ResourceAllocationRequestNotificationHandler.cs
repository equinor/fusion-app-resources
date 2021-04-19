using Fusion.Integration.Notification;
using Fusion.Integration.Org;
using Fusion.Resources.Api.Notifications.Markdown;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Queries;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.ApiClients.Org;
using Fusion.Resources.Domain.Notifications;

namespace Fusion.Resources.Api.Notifications
{
    public class ResourceAllocationRequestNotificationHandler :
        INotificationHandler<ResourceAllocationRequestProvisioned>,
        INotificationHandler<ResourceAllocationRequestAllocatedPersonProposal>,
        INotificationHandler<ResourceAllocationRequestAssignedPersonAccepted>,
        INotificationHandler<ResourceAllocationRequestTaskOwnerAssigned>,
        INotificationHandler<ResourceAllocationRequestChanged>
    {
        private readonly IMediator mediator;
        private readonly IFusionNotificationClient notificationClient;
        private readonly IProjectOrgResolver orgResolver;
        private static string DefaultFollowUpText => "Please review and follow up request in Resource Allocation";

        public ResourceAllocationRequestNotificationHandler(IMediator mediator, IFusionNotificationClient notificationClient, IProjectOrgResolver orgResolver)
        {
            this.mediator = mediator;
            this.notificationClient = notificationClient;
            this.orgResolver = orgResolver;
        }

        public async Task Handle(ResourceAllocationRequestProvisioned notification, CancellationToken cancellationToken)
        {
            var request = await GetResolvedOrgData(notification.RequestId);
            var recipients = GenerateTaskOwnerRecipients(request.Instance);

            foreach (var recipient in recipients)
            {
                NotificationArguments arguments = new($"Workflow for position {request.Position.Name} changed") { Priority = EmailPriority.Low };

                await notificationClient.CreateNotificationForUserAsync(recipient, arguments, builder =>
                {
                    builder.AddDescription(NotificationDescription.RequestWorkflowChanged(request.Position, request.Instance));
                });
            }
        }

        public async Task Handle(ResourceAllocationRequestAllocatedPersonProposal notification, CancellationToken cancellationToken)
        {
            var request = await GetResolvedOrgData(notification.RequestId);
            var recipients = GenerateTaskOwnerRecipients(request.Instance);

            foreach (var recipient in recipients)
            {
                NotificationArguments arguments = new($"Person allocation for position {request.Position.Name} proposed") { Priority = EmailPriority.Low };

                await notificationClient.CreateNotificationForUserAsync(recipient, arguments, builder =>
                {
                    builder.AddDescription(NotificationDescription.RequestPersonProposal(request.Position, request.Instance));
                });
            }
        }

        public async Task Handle(ResourceAllocationRequestAssignedPersonAccepted notification, CancellationToken cancellationToken)
        {
            var request = await GetResolvedOrgData(notification.RequestId);
            var recipients = GenerateTaskOwnerRecipients(request.Instance);

            foreach (var recipient in recipients)
            {
                NotificationArguments arguments = new($"Person allocation for position {request.Position.Name} accepted") { Priority = EmailPriority.Low };

                await notificationClient.CreateNotificationForUserAsync(recipient, arguments, builder =>
                {
                    builder.AddDescription(NotificationDescription.RequestAssignedPersonAccepted(request.Position, request.Instance));
                });
            }
        }

        public async Task Handle(ResourceAllocationRequestTaskOwnerAssigned notification, CancellationToken cancellationToken)
        {
            var request = await GetResolvedOrgData(notification.RequestId);
            var recipients = GenerateTaskOwnerRecipients(request.Instance);

            foreach (var recipient in recipients)
            {
                NotificationArguments arguments = new($"Workflow assigned for position {request.Position.Name}") { Priority = EmailPriority.Low };

                await notificationClient.CreateNotificationForUserAsync(recipient, arguments, builder =>
                {
                    builder.AddDescription(NotificationDescription.RequestWorkflowAssignedToTaskOwner(request.Position, request.Instance));
                });
            }
        }
        public async Task Handle(ResourceAllocationRequestChanged notification, CancellationToken cancellationToken)
        {
            var request = await GetResolvedOrgData(notification.RequestId);
            var recipients = GenerateTaskOwnerRecipients(request.Instance);

            foreach (var recipient in recipients)
            {
                NotificationArguments arguments = new($"Request for position {request.Position.Name} changed") { Priority = EmailPriority.Low };

                await notificationClient.CreateNotificationForUserAsync(recipient, arguments, builder =>
                {
                    builder.AddDescription(NotificationDescription.RequestChanged(request.Position, request.Instance));
                });
            }
        }

        private async Task<NotificationRequestData> GetResolvedOrgData(Guid requestId)
        {
            var internalRequest = await GetInternalRequestAsync(requestId);
            if (internalRequest is null)
                throw new InvalidOperationException($"Internal request {requestId} not found");

            var orgPosition = await orgResolver.ResolvePositionAsync(internalRequest.OrgPositionId.GetValueOrDefault());
            if (orgPosition == null)
                throw new InvalidOperationException($"Cannot resolve position for request {internalRequest.RequestId}");

            var orgPositionInstance = orgPosition.Instances.SingleOrDefault(x => x.Id == internalRequest.OrgPositionInstanceId);
            if (orgPositionInstance == null)
                throw new InvalidOperationException($"Cannot resolve position instance for request {internalRequest.RequestId}");

            return new NotificationRequestData(orgPosition, orgPositionInstance);
        }
        private async Task<QueryResourceAllocationRequest?> GetInternalRequestAsync(Guid requestId)
        {
            var query = new GetResourceAllocationRequestItem(requestId);
            var request = await mediator.Send(query);

            return request;
        }
        private static IEnumerable<Guid> GenerateTaskOwnerRecipients(ApiPositionInstanceV2 instance)
        {
            var recipients = new List<Guid>();
            recipients.AddRange(instance.TaskOwnerIds);
            return recipients;
        }

        private static class NotificationDescription
        {
            public static string RequestPersonProposal(ApiPositionV2 orgPosition, ApiPositionInstanceV2 orgPositionInstance) => new MarkdownDocument()
                .Paragraph($"{orgPositionInstance.AssignedPerson.Name} ({orgPositionInstance.AssignedPerson.Mail}) was proposed for position {orgPosition.Name}.")
                .Paragraph(DefaultFollowUpText)
                .List(l => l
                    .ListItem($"{MdToken.Bold("Project:")} {orgPosition.Project.Name}")
                    .ListItem($"{MdToken.Bold("Position:")} {orgPosition.Name}")
                )
                .Build();

            public static string RequestAssignedPersonAccepted(ApiPositionV2 orgPosition, ApiPositionInstanceV2 orgPositionInstance) => new MarkdownDocument()
                .Paragraph($"{orgPositionInstance.AssignedPerson.Name} ({orgPositionInstance.AssignedPerson.Mail}) was accepted for position {orgPosition.Name}.")
                .Paragraph(DefaultFollowUpText)
                .List(l => l
                    .ListItem($"{MdToken.Bold("Project:")} {orgPosition.Project.Name}")
                    .ListItem($"{MdToken.Bold("Position:")} {orgPosition.Name}")
                )
                .Build();

            public static string RequestWorkflowAssignedToTaskOwner(ApiPositionV2 orgPosition, ApiPositionInstanceV2 orgPositionInstance) => new MarkdownDocument()
                .Paragraph($"Workflow assigned for position {orgPosition.Name}.")
                .Paragraph(DefaultFollowUpText)
                .List(l => l
                    .ListItem($"{MdToken.Bold("Project:")} {orgPosition.Project.Name}")
                    .ListItem($"{MdToken.Bold("Position:")} {orgPosition.Name}")
                )
                .Build();

            public static string RequestWorkflowChanged(ApiPositionV2 orgPosition, ApiPositionInstanceV2 orgPositionInstance) => new MarkdownDocument()
                .Paragraph($"Workflow changed for position {orgPosition.Name}.")
                .Paragraph(DefaultFollowUpText)
                .List(l => l
                    .ListItem($"{MdToken.Bold("Project:")} {orgPosition.Project.Name}")
                    .ListItem($"{MdToken.Bold("Position:")} {orgPosition.Name}")
                )
                .Build();

            public static string RequestChanged(ApiPositionV2 orgPosition, ApiPositionInstanceV2 orgPositionInstance) => new MarkdownDocument()
                .Paragraph($"Request changed for position {orgPosition.Name}.")
                .Paragraph(DefaultFollowUpText)
                .List(l => l
                    .ListItem($"{MdToken.Bold("Project:")} {orgPosition.Project.Name}")
                    .ListItem($"{MdToken.Bold("Position:")} {orgPosition.Name}")
                )
                .Build();
        }
        
        private class NotificationRequestData
        {
            public NotificationRequestData(ApiPositionV2 position, ApiPositionInstanceV2 instance)
            {
                Position = position;
                Instance = instance;
            }
            
            public ApiPositionV2 Position { get; }
            public ApiPositionInstanceV2 Instance { get; }
        }
    }
}
