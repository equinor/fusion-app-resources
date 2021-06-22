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
using Fusion.Resources.Database;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Resources.Api.Notifications
{
    public partial class InternalRequestNotification
    {
        public class RequestInitializedHandler : INotificationHandler<Logic.Commands.ResourceAllocationRequest.RequestInitialized>
        {
            private readonly IMediator mediator;
            private readonly IFusionNotificationClient notificationClient;
            private readonly IProjectOrgResolver orgResolver;
            private readonly IFusionContextResolver contextResolver;
            private readonly ResourcesDbContext dbContext;

            public RequestInitializedHandler(IMediator mediator, IFusionNotificationClient notificationClient,
                IProjectOrgResolver orgResolver, IFusionContextResolver contextResolver, ResourcesDbContext dbContext)
            {
                this.mediator = mediator;
                this.notificationClient = notificationClient;
                this.orgResolver = orgResolver;
                this.contextResolver = contextResolver;
                this.dbContext = dbContext;
            }

            public async Task Handle(Logic.Commands.ResourceAllocationRequest.RequestInitialized notification, CancellationToken cancellationToken)
            {
                var request = await GetResolvedOrgDataAsync(notification.RequestId, notification.GetType());
                var recipients = await GenerateRecipientsAsync(notification.InitiatedByDbPersonId, request);

                foreach (var recipient in recipients)
                {
                    var arguments = new NotificationArguments($"Request for position {request.Position.Name} changed") { Priority = EmailPriority.Low };

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
                                .TryAddOpenPortalUrlAction("Open request", $"{request.PortalUrl}")
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
                                .TryAddOpenPortalUrlAction("Open request", $"{request.PortalUrl}")
                                ;
                        });
                    }
                }
            }

            private async Task<NotificationRequestData> GetResolvedOrgDataAsync(Guid requestId, Type notificationType)
            {
                var internalRequest = await GetInternalRequestAsync(requestId);
                if (internalRequest is null)
                    throw new InvalidOperationException($"Internal request {requestId} not found");

                var orgPosition =
                    await orgResolver.ResolvePositionAsync(internalRequest.OrgPositionId.GetValueOrDefault());
                if (orgPosition == null)
                    throw new InvalidOperationException(
                        $"Cannot resolve position for request {internalRequest.RequestId}");

                var orgPositionInstance =
                    orgPosition.Instances.SingleOrDefault(x => x.Id == internalRequest.OrgPositionInstanceId);
                if (orgPositionInstance == null)
                    throw new InvalidOperationException(
                        $"Cannot resolve position instance for request {internalRequest.RequestId}");

                var context = await contextResolver.ResolveContextAsync(
                    ContextIdentifier.FromExternalId(internalRequest.Project.OrgProjectId), FusionContextType.OrgChart);
                var orgContextId = $"{context?.Id}";

                return new NotificationRequestData(notificationType, internalRequest, orgPosition, orgPositionInstance)
                    .WithContextId(orgContextId)
                    .WithPortalActionUrls();
            }



            private async Task<QueryResourceAllocationRequest?> GetInternalRequestAsync(Guid requestId)
            {
                var query = new GetResourceAllocationRequestItem(requestId).ExpandTaskOwner();
                var request = await mediator.Send(query);

                return request;
            }

            private async Task<IEnumerable<Guid>> GenerateRecipientsAsync(Guid notificationInitiatedByPersonId,
                NotificationRequestData data)
            {
                var recipients = new List<Guid>();
                var notificationInitiatedBy =
                    await dbContext.Persons.FirstOrDefaultAsync(p => p.Id == notificationInitiatedByPersonId);

                if (data.NotifyCreator && data.AllocationRequest.CreatedBy.AzureUniqueId !=
                    notificationInitiatedBy?.AzureUniqueId)
                    recipients.Add(data.AllocationRequest.CreatedBy.AzureUniqueId);

                if (data.NotifyResourceOwner && data.Instance.AssignedPerson?.AzureUniqueId != null)
                {
                    var ro = await mediator.Send(new GetResourceOwner(data.Instance.AssignedPerson.AzureUniqueId.Value));
                    if (ro?.IsResourceOwner == true && ro.AzureUniqueId.HasValue &&
                        ro.AzureUniqueId != notificationInitiatedBy?.AzureUniqueId)
                        recipients.Add(ro.AzureUniqueId.Value);
                }

                if (data.NotifyTaskOwner)
                {
                    var taskOwnerPersonIds = data.AllocationRequest.TaskOwner?.Persons?
                        .Where(x => x.AzureUniqueId != null &&
                                    x.AzureUniqueId != notificationInitiatedBy?.AzureUniqueId)
                        .Select(taskOwnerPerson => taskOwnerPerson.AzureUniqueId!.Value) ?? new List<Guid>();
                    recipients.AddRange(taskOwnerPersonIds);


                }

                return recipients.Distinct(); // A person may be a creator and/or resource owner and/or task owner.
            }

            private class NotificationRequestData
            {
                public NotificationRequestData(Type notificationType, QueryResourceAllocationRequest allocationRequest, ApiPositionV2 position, ApiPositionInstanceV2 instance)
                {
                    AllocationRequest = allocationRequest;
                    Position = position;
                    Instance = instance;
                    NotificationType = notificationType;

                    DecideWhoShouldBeNotified(allocationRequest);
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
                public Type NotificationType { get; }
                public string? PortalUrl { get; private set; }

                public NotificationRequestData WithContextId(string? contextId)
                {
                    OrgContextId = contextId;
                    return this;
                }

                public NotificationRequestData WithPortalActionUrls()
                {
                    if (!string.IsNullOrEmpty(OrgContextId))
                    {
                        PortalUrl = NotificationType.Name switch
                        {
                            nameof(Logic.Commands.ResourceAllocationRequest.RequestInitialized) =>
                                $"/apps/org-admin/{OrgContextId}/timeline?instanceId={Instance.Id}&positionId={Position.Id}",
                            _ => $"/apps/org-admin/{OrgContextId}"
                        };
                    }
                    return this;
                }

                private void DecideWhoShouldBeNotified(QueryResourceAllocationRequest allocationRequest)
                {

                    IsAllocationRequest = allocationRequest.Type == InternalRequestType.Allocation;
                    IsChangeRequest = allocationRequest.Type == InternalRequestType.ResourceOwnerChange;

                    switch (NotificationType.Name)
                    {
                        case nameof(Logic.Commands.ResourceAllocationRequest.RequestInitialized):
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
}