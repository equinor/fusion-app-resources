using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.ApiClients.Org;
using Fusion.Integration;
using Fusion.Integration.Notification;
using Fusion.Integration.Org;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands;
using Fusion.Resources.Domain.Notifications.InternalRequests;
using Fusion.Resources.Domain.Queries;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Fusion.Resources.Api.Notifications
{

    public partial class InternalRequestNotification
    {
        public class ProposedPersonHandler : INotificationHandler<InternalRequestNotifications.ProposedPerson>
        {
            private readonly IMediator mediator;
            private readonly INotificationBuilder notificationBuilder;
            private readonly IProjectOrgResolver orgResolver;
            private readonly IFusionContextResolver contextResolver;
            private readonly ILogger<InternalRequestNotification> logger;

            public ProposedPersonHandler(IMediator mediator, INotificationBuilderFactory notificationClient, IProjectOrgResolver orgResolver, IFusionContextResolver contextResolver, ILogger<InternalRequestNotification> logger)
            {
                this.mediator = mediator;
                this.notificationBuilder = notificationClient.CreateDesigner();
                this.orgResolver = orgResolver;
                this.contextResolver = contextResolver;
                this.logger = logger;
            }

            public async Task Handle(InternalRequestNotifications.ProposedPerson notification, CancellationToken cancellationToken)
            {
                var request = await GetResolvedOrgDataAsync(notification.RequestId);

                if (request.AllocationRequest.ProposedPerson is null || request.AllocationRequest.TaskOwner?.Persons?.Any() == false)
                    return;

                try
                {
                    notificationBuilder.AddTitle("A personnel request has been assigned to you")
                    .AddTextBlockIf("Proposed resource", request.Instance.AssignedPerson != null)
                    .TryAddProfileCard(request.Instance.AssignedPerson?.AzureUniqueId)

                    .AddDescription("Please review and handle request")

                    .AddFacts(facts => facts
                        .AddFactIf("Project", request.Position.Project.Name, request.Position?.Project != null)
                        .AddFact("Position", request.Position!.Name)
                        .AddFact("Period", $"{request.Instance.AppliesFrom:dd.MM.yyyy} - {request.Instance.AppliesTo:dd.MM.yyyy}") // Until we have resolved date formatting issue related to timezone.
                        .AddFact("Workload", $"{request.Instance?.Workload}")
                        )
                    .AddTextBlock($"Created by: {request.AllocationRequest.CreatedBy.Name}")
                    .TryAddOpenPortalUrlAction("Open position in org admin", $"{request.OrgPortalUrl}");

                    var card = await notificationBuilder.BuildCardAsync();
                    await mediator.Send(new NotifyTaskOwner(request.AllocationRequest.RequestId, card));
                    await mediator.Send(new NotifyRequestCreator(request.AllocationRequest.RequestId, card));

                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                }
            }

            private async Task<NotificationRequestData> GetResolvedOrgDataAsync(Guid requestId)
            {
                var internalRequest = await GetInternalRequestAsync(requestId);
                if (internalRequest is null)
                    throw new InvalidOperationException($"Internal request {requestId} not found");

                var orgPosition = await orgResolver.ResolvePositionAsync(internalRequest.OrgPositionId.GetValueOrDefault());
                if (orgPosition == null)
                    throw new InvalidOperationException(
                        $"Cannot resolve position for request {internalRequest.RequestId}");

                var orgPositionInstance = orgPosition.Instances.SingleOrDefault(x => x.Id == internalRequest.OrgPositionInstanceId);
                if (orgPositionInstance == null)
                    throw new InvalidOperationException(
                        $"Cannot resolve position instance for request {internalRequest.RequestId}");

                var context = await contextResolver.ResolveContextAsync(ContextIdentifier.FromExternalId(internalRequest.Project.OrgProjectId), FusionContextType.OrgChart);
                var orgContextId = $"{context?.Id}";

                return new NotificationRequestData(internalRequest, orgPosition, orgPositionInstance)
                    .WithContextId(orgContextId)
                    .WithPortalActionUrls();
            }

            private async Task<QueryResourceAllocationRequest?> GetInternalRequestAsync(Guid requestId)
            {
                var query = new GetResourceAllocationRequestItem(requestId).ExpandTaskOwner();
                var request = await mediator.Send(query);
                return request;
            }

            private class NotificationRequestData
            {
                public NotificationRequestData(QueryResourceAllocationRequest allocationRequest, ApiPositionV2 position,
                    ApiPositionInstanceV2 instance)
                {
                    AllocationRequest = allocationRequest;
                    Position = position;
                    Instance = instance;
                }

                private string? OrgContextId { get; set; }
                public QueryResourceAllocationRequest AllocationRequest { get; }
                public ApiPositionV2 Position { get; }
                public ApiPositionInstanceV2 Instance { get; }
                public string? OrgPortalUrl { get; private set; }

                public NotificationRequestData WithContextId(string? contextId)
                {
                    OrgContextId = contextId;
                    return this;
                }

                public NotificationRequestData WithPortalActionUrls()
                {
                    if (!string.IsNullOrEmpty(OrgContextId))
                    {
                        OrgPortalUrl = $"aka/goto-org-admin/{OrgContextId}/{Position.Id}/{Instance.Id}";
                    }

                    return this;
                }
            }
        }
    }
}