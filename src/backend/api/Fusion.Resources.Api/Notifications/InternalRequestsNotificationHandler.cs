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
using Fusion.Resources.Logic.Workflows;

namespace Fusion.Resources.Api.Notifications
{
    public class InternalRequestsNotificationHandler :
        INotificationHandler<WorkflowChanged>,
        INotificationHandler<AllocatedPersonProposal>,
        INotificationHandler<AssignedPersonAccepted>,
        INotificationHandler<TaskOwnerAssigned>,
        INotificationHandler<RequestChanged>
    {
        private readonly IMediator mediator;
        private readonly IFusionNotificationClient notificationClient;
        private readonly IProjectOrgResolver orgResolver;
        private static string DefaultFollowUpText => "Please review and follow up request in Resource Allocation";

        public InternalRequestsNotificationHandler(IMediator mediator, IFusionNotificationClient notificationClient, IProjectOrgResolver orgResolver)
        {
            this.mediator = mediator;
            this.notificationClient = notificationClient;
            this.orgResolver = orgResolver;
        }

        public async Task Handle(WorkflowChanged notification, CancellationToken cancellationToken)
        {
            var request = await GetResolvedOrgData(notification.RequestId, notification.GetType());
            var recipients = await GenerateRecipientsAsync(request);

            foreach (var recipient in recipients)
            {
                NotificationArguments arguments = new($"Workflow for position {request.Position.Name} changed") { Priority = EmailPriority.Low };

                await notificationClient.CreateNotificationForUserAsync(recipient, arguments, builder =>
                {
                    builder.AddDescription(NotificationDescription.RequestWorkflowChanged(request.Position, request.Instance));
                });
            }
        }

        public async Task Handle(AllocatedPersonProposal notification, CancellationToken cancellationToken)
        {
            var request = await GetResolvedOrgData(notification.RequestId, notification.GetType());
            var recipients = await GenerateRecipientsAsync(request);

            foreach (var recipient in recipients)
            {
                NotificationArguments arguments = new($"Person allocation for position {request.Position.Name} proposed") { Priority = EmailPriority.Low };

                await notificationClient.CreateNotificationForUserAsync(recipient, arguments, builder =>
                {
                    builder.AddDescription(NotificationDescription.RequestPersonProposal(request.Position, request.Instance));
                });
            }
        }

        public async Task Handle(AssignedPersonAccepted notification, CancellationToken cancellationToken)
        {
            var request = await GetResolvedOrgData(notification.RequestId, notification.GetType());
            var recipients = await GenerateRecipientsAsync(request);

            foreach (var recipient in recipients)
            {
                NotificationArguments arguments = new($"Person allocation for position {request.Position.Name} accepted") { Priority = EmailPriority.Low };

                await notificationClient.CreateNotificationForUserAsync(recipient, arguments, builder =>
                {
                    builder.AddDescription(NotificationDescription.RequestAssignedPersonAccepted(request.Position, request.Instance));
                });
            }
        }

        public async Task Handle(TaskOwnerAssigned notification, CancellationToken cancellationToken)
        {
            var request = await GetResolvedOrgData(notification.RequestId, notification.GetType());
            var recipients = await GenerateRecipientsAsync(request);

            foreach (var recipient in recipients)
            {
                NotificationArguments arguments = new($"Workflow assigned for position {request.Position.Name}") { Priority = EmailPriority.Low };

                await notificationClient.CreateNotificationForUserAsync(recipient, arguments, builder =>
                {
                    builder.AddDescription(NotificationDescription.RequestWorkflowAssignedToTaskOwner(request.Position, request.Instance));
                });
            }
        }
        public async Task Handle(RequestChanged notification, CancellationToken cancellationToken)
        {
            var request = await GetResolvedOrgData(notification.RequestId, notification.GetType());
            var recipients = await GenerateRecipientsAsync(request);

            foreach (var recipient in recipients)
            {
                NotificationArguments arguments = new($"Request for position {request.Position.Name} changed") { Priority = EmailPriority.Low };

                await notificationClient.CreateNotificationForUserAsync(recipient, arguments, builder =>
                {
                    builder.AddDescription(NotificationDescription.RequestChanged(request.Position, request.Instance));
                });
            }
        }

        private async Task<NotificationRequestData> GetResolvedOrgData(Guid requestId, Type notificationType)
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

            return new NotificationRequestData(notificationType, internalRequest, orgPosition, orgPositionInstance);
        }
        private async Task<QueryResourceAllocationRequest?> GetInternalRequestAsync(Guid requestId)
        {
            var query = new GetResourceAllocationRequestItem(requestId);
            var request = await mediator.Send(query);

            return request;
        }
        private async Task<IEnumerable<Guid>> GenerateRecipientsAsync(NotificationRequestData data)
        {
            var recipients = new List<Guid>();

            if (data.NotifyCreator)
                recipients.Add(data.AllocationRequest.CreatedBy.AzureUniqueId);

            if (data.NotifyResourceOwner && data.Instance.AssignedPerson?.AzureUniqueId != null)
            {
                var ro = await mediator.Send(new GetResourceOwner(data.Instance.AssignedPerson.AzureUniqueId.Value));
                if (ro?.IsResourceOwner == true && ro.AzureUniqueId.HasValue)
                    recipients.Add(ro.AzureUniqueId.Value);
            }

            if (data.NotifyTaskOwner && data.Instance.TaskOwnerIds != null)
                recipients.AddRange(data.Instance.TaskOwnerIds);

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
            public NotificationRequestData(Type notificationType, QueryResourceAllocationRequest allocationRequest,
                ApiPositionV2 position, ApiPositionInstanceV2 instance)
            {
                AllocationRequest = allocationRequest;
                Position = position;
                Instance = instance;

                DecideWhoShouldBeNotified(notificationType, allocationRequest);
            }

            private void DecideWhoShouldBeNotified(Type notificationType, QueryResourceAllocationRequest allocationRequest)
            {

                bool isAllocationRequest = allocationRequest.Type == InternalRequestType.Allocation;
                bool isChangeRequest = allocationRequest.Type == InternalRequestType.ResourceOwnerChange;

                var isDirect = allocationRequest.SubType == AllocationDirectWorkflowV1.SUBTYPE;
                var isJointVenture = allocationRequest.SubType == AllocationJointVentureWorkflowV1.SUBTYPE;
                var isNormal = allocationRequest.SubType == AllocationNormalWorkflowV1.SUBTYPE;



                switch (notificationType.Name)
                {
                    //Provision
                    case nameof(WorkflowChanged):
                        if (isAllocationRequest)
                        {
                            if (isNormal)
                            {
                                NotifyTaskOwner = true;
                                NotifyCreator = true;
                                NotifyResourceOwner = true;
                            }
                            else if (isDirect)
                            {
                                NotifyTaskOwner = true;
                                NotifyCreator = true;
                            }
                            else if (isJointVenture)
                            {
                                NotifyTaskOwner = true;
                                NotifyCreator = true;
                            }
                        }
                        if (isChangeRequest)
                        {
                            NotifyTaskOwner = true;
                            NotifyResourceOwner = true;
                        }
                        break;
                    case nameof(AllocatedPersonProposal):
                        if (isAllocationRequest)
                        {
                            NotifyTaskOwner = true;
                            NotifyCreator = true;
                        }
                        else if (isChangeRequest)
                        {
                            NotifyTaskOwner = true;
                        }
                        break;
                    case nameof(AssignedPersonAccepted):
                        if (isAllocationRequest)
                        {
                            NotifyResourceOwner = true;
                        }
                        else if (isChangeRequest)
                        {
                            NotifyResourceOwner = true;
                        }

                        break;
                    case nameof(TaskOwnerAssigned):
                        if (isAllocationRequest)
                        {
                            NotifyTaskOwner = true;
                            NotifyCreator = true;
                        }
                        break;
                    case nameof(RequestChanged):
                        if (isChangeRequest)
                        {
                            NotifyTaskOwner = true;
                            NotifyResourceOwner = true;
                        }
                        break;
                }
            }

            public bool NotifyResourceOwner { get; private set; }
            public bool NotifyTaskOwner { get; private set; }
            public bool NotifyCreator { get; private set; }
            public QueryResourceAllocationRequest AllocationRequest { get; }
            public ApiPositionV2 Position { get; }
            public ApiPositionInstanceV2 Instance { get; }
        }
    }
}
