using Fusion.Integration.Notification;
using Fusion.Integration.Org;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Queries;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.ApiClients.Org;
using Fusion.Integration;
using Fusion.Resources.Logic.Workflows;
using ResourceAllocationRequest = Fusion.Resources.Logic.Commands.ResourceAllocationRequest;

namespace Fusion.Resources.Api.Notifications
{
    public class InternalRequestsNotificationHandler :
        INotificationHandler<ResourceAllocationRequest.RequestInitialized>
    {
        private readonly IMediator mediator;
        private readonly IFusionNotificationClient notificationClient;
        private readonly IProjectOrgResolver orgResolver;
        private readonly IFusionContextResolver contextResolver;

        public InternalRequestsNotificationHandler(IMediator mediator, IFusionNotificationClient notificationClient, IProjectOrgResolver orgResolver, IFusionContextResolver contextResolver)
        {
            this.mediator = mediator;
            this.notificationClient = notificationClient;
            this.orgResolver = orgResolver;
            this.contextResolver = contextResolver;
        }
        public async Task Handle(ResourceAllocationRequest.RequestInitialized notification, CancellationToken cancellationToken)
        {
            var request = await GetResolvedOrgData(notification.RequestId, notification.GetType());
            var recipients = await GenerateRecipientsAsync(request);

            foreach (var recipient in recipients)
            {
                NotificationArguments arguments = new($"Request for position {request.Position.Name} changed") { Priority = EmailPriority.Low };

                if (request.Instance.AssignedPerson is null)
                {

                    await notificationClient.CreateNotificationForUserAsync(recipient, arguments, builder =>
                    {
                        builder
                            .AddDescription("Please review and follow up request")
                            .AddFacts(facts => facts
                                .AddFact("Project", request.Position.Project.Name)
                                .AddFact("Request created by", request.AllocationRequest.CreatedBy.Name)
                            )
                            .TryAddOpenPortalUrlAction("Open request", request.PortalUrl)
                            ;
                    });
                }
                else // Person assigned, try to add profile card
                {
                    await notificationClient.CreateNotificationForUserAsync(recipient, arguments, builder =>
                    {
                        builder
                            .TryAddProfileCard(request.Instance.AssignedPerson.AzureUniqueId)
                            .AddDescription(
                                $"{request.Instance.AssignedPerson.Name} ({request.Instance.AssignedPerson.Mail}) was proposed for position {request.Position.Name}.")
                            .AddFacts(facts => facts
                                .AddFact("Project", request.Position.Project.Name)
                                .AddFact("Request created by", request.AllocationRequest.CreatedBy.Name)
                            )
                            .TryAddOpenPortalUrlAction("Open request", request.PortalUrl)
                            ;
                    });
                }
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

            var context = await contextResolver.ResolveContextAsync(ContextIdentifier.FromExternalId(internalRequest.Project.OrgProjectId), FusionContextType.OrgChart);
            var orgContextId = $"{context?.Id}";

            return new NotificationRequestData(notificationType, internalRequest, orgPosition, orgPositionInstance).WithContextId(orgContextId);
        }



        private async Task<QueryResourceAllocationRequest?> GetInternalRequestAsync(Guid requestId)
        {
            var query = new GetResourceAllocationRequestItem(requestId)
            {
                Expands = GetResourceAllocationRequestItem.ExpandProperties.TaskOwner
            }; ;
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

            if (data.NotifyTaskOwner)
            {
                var taskOwnerPersonIds = data.AllocationRequest.TaskOwner?.Persons?.Where(x => x.AzureUniqueId != null).Select(taskOwnerPerson => taskOwnerPerson.AzureUniqueId!.Value) ?? new List<Guid>();
                recipients.AddRange(taskOwnerPersonIds);


            }

            return recipients.Distinct();// A person may be a creator and/or resource owner and/or task owner.
        }

        private class NotificationRequestData
        {
            public NotificationRequestData(Type notificationType, QueryResourceAllocationRequest allocationRequest, ApiPositionV2 position, ApiPositionInstanceV2 instance)
            {
                AllocationRequest = allocationRequest;
                Position = position;
                Instance = instance;

                DecideWhoShouldBeNotified(notificationType, allocationRequest);

                if (!string.IsNullOrEmpty(OrgContextId))
                {
                    PortalUrl = notificationType.Name switch
                    {
                        nameof(ResourceAllocationRequest.RequestInitialized) =>
                            $"/apps/org-admin/{OrgContextId}/timeline?instanceId={Instance.Id}&positionId={Position.Id}",
                        _ => $"/apps/org-admin/{OrgContextId}"
                    };
                }
            }

            private string? OrgContextId { get; set; }
            private bool IsAllocationRequest { get; set; }
            private bool IsChangeRequest { get; set; }

            public bool NotifyResourceOwner { get; private set; }
            public bool NotifyTaskOwner { get; private set; }
            public bool NotifyCreator { get; private set; }
            public QueryResourceAllocationRequest AllocationRequest { get; }
            public ApiPositionV2 Position { get; }
            public ApiPositionInstanceV2 Instance { get; }
            public string PortalUrl { get; } = "/apps/org-admin/";


            public NotificationRequestData WithContextId(string? contextId)
            {
                OrgContextId = contextId;
                return this;
            }
            private void DecideWhoShouldBeNotified(Type notificationType, QueryResourceAllocationRequest allocationRequest)
            {

                IsAllocationRequest = allocationRequest.Type == InternalRequestType.Allocation;
                IsChangeRequest = allocationRequest.Type == InternalRequestType.ResourceOwnerChange;

                var isDirect = allocationRequest.SubType == AllocationDirectWorkflowV1.SUBTYPE;
                var isJointVenture = allocationRequest.SubType == AllocationJointVentureWorkflowV1.SUBTYPE;
                var isNormal = allocationRequest.SubType == AllocationNormalWorkflowV1.SUBTYPE;



                switch (notificationType.Name)
                {
                    //What and who
                    /* case nameof(WorkflowChanged):
                         if (IsAllocationRequest)
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
                         else if (IsChangeRequest)
                         {
                             NotifyTaskOwner = true;
                             NotifyResourceOwner = true;
                         }
                         break;
                     case nameof(ProposedPersonChanged):
                         if (IsAllocationRequest)
                         {
                             NotifyTaskOwner = true;
                             NotifyCreator = true;
                         }
                         else if (IsChangeRequest)
                         {
                             NotifyTaskOwner = true;
                         }
                         break;
                     case nameof(AssignedPersonAccepted):
                         if (IsAllocationRequest)
                         {
                             NotifyResourceOwner = true;
                         }
                         else if (IsChangeRequest)
                         {
                             NotifyResourceOwner = true;
                         }
                         break;*/
                    case nameof(ResourceAllocationRequest.RequestInitialized):
                        if (IsAllocationRequest)
                        {
                            NotifyTaskOwner = true;
                            NotifyCreator = true;
                        }
                        else if (IsChangeRequest)
                        {
                            NotifyTaskOwner = true;
                        }
                        break;
                }
            }
        }
    }
}
