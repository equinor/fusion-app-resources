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

namespace Fusion.Resources.Api.Notifications;

public partial class InternalRequestNotification
{
    public class
        ProposedPersonAutoAcceptedHandler : INotificationHandler<
        InternalRequestNotifications.ProposedPersonAutoAccepted>
    {
        private readonly IMediator mediator;
        private readonly INotificationBuilder notificationBuilder;
        private readonly IProjectOrgResolver orgResolver;
        private readonly IFusionContextResolver contextResolver;
        private readonly ILogger<ProposedPersonAutoAcceptedHandler> logger;


        public ProposedPersonAutoAcceptedHandler(IMediator mediator,
            INotificationBuilderFactory notificationBuilderFactory, IProjectOrgResolver orgResolver,
            IFusionContextResolver contextResolver, ILogger<ProposedPersonAutoAcceptedHandler> logger)
        {
            this.mediator = mediator;
            this.orgResolver = orgResolver;
            this.contextResolver = contextResolver;
            this.logger = logger;
            this.notificationBuilder = notificationBuilderFactory.CreateDesigner();
        }

        public async Task Handle(InternalRequestNotifications.ProposedPersonAutoAccepted notification,
            CancellationToken cancellationToken)
        {
            try
            {
                var requestId = notification.RequestId;
                var request = await GetResolvedOrgDataAsync(requestId);


                var card = await notificationBuilder
                    .AddTitle($"A {request.AllocationRequest.SubType} personnel allocation request was auto-accepted")
                    .AddDescription(
                        $"The {request.AllocationRequest.SubType} personnel request has been auto-accepted as there where no proposed changes by the Resource owner. " +
                        $"Changes wil be provisioned to the org chart")
                    .AddFacts(facts => facts
                        .AddFact("Request number", $"{request.AllocationRequest.RequestNumber}")
                        .AddFact("Project", request.Position.Project.Name)
                        .AddFact("Position id", request.Position.ExternalId)
                        .AddFact("Position", request.Position.Name)
                        .AddFact("Period", $"{request.Instance.GetFormattedPeriodString()}")
                        .AddFact("Workload", $"{request.Instance.GetFormattedWorkloadString()}")
                    )
                    .AddTextBlockIf($"Additional comment: {request.AllocationRequest.AdditionalNote.TrimText(500)}",
                        !string.IsNullOrEmpty(request.AllocationRequest.AdditionalNote))
                    .AddTextBlock($"Created by: {request.AllocationRequest.CreatedBy.Name}")
                    .TryAddOpenPortalUrlAction("Open position in org admin", $"{request.OrgPortalUrl}")
                    .BuildCardAsync();

                card.AdditionalProperties.Add("requestId", $"{requestId}");

                await mediator.Send(new NotifyTaskOwner(requestId, card), cancellationToken);
                await mediator.Send(new NotifyRequestCreator(requestId, card), cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to send notification for auto-approved request {RequestId}",
                    notification.RequestId);
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

            var orgPositionInstance =
                orgPosition.Instances.SingleOrDefault(x => x.Id == internalRequest.OrgPositionInstanceId);
            if (orgPositionInstance == null)
                throw new InvalidOperationException(
                    $"Cannot resolve position instance for request {internalRequest.RequestId}");

            var context = await contextResolver.ResolveContextAsync(
                ContextIdentifier.FromExternalId(internalRequest.Project.OrgProjectId), FusionContextType.OrgChart);
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