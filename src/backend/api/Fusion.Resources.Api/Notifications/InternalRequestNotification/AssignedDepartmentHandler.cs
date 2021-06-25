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
        public class AssignedDepartmentHandler : INotificationHandler<InternalRequestNotifications.AssignedDepartment>
        {
            private readonly IMediator mediator;
            private readonly INotificationBuilder notificationBuilder;
            private readonly IProjectOrgResolver orgResolver;
            private readonly IFusionContextResolver contextResolver;
            private readonly ILogger<InternalRequestNotification> logger;

            public AssignedDepartmentHandler(IMediator mediator, INotificationBuilderFactory notificationClient, IProjectOrgResolver orgResolver, IFusionContextResolver contextResolver, ILogger<InternalRequestNotification> logger)
            {
                this.mediator = mediator;
                this.notificationBuilder = notificationClient.CreateDesigner();
                this.orgResolver = orgResolver;
                this.contextResolver = contextResolver;
                this.logger = logger;
            }

            public async Task Handle(InternalRequestNotifications.AssignedDepartment notification, CancellationToken cancellationToken)
            {
                var request = await GetResolvedOrgDataAsync(notification.RequestId);

                if (string.IsNullOrEmpty(request.AllocationRequest.AssignedDepartment))
                    return;

                try
                {
                    notificationBuilder.AddTitle("A personnel request has been assigned to you")
                    .AddTextBlock("Task owner")
                    .TryAddProfileCard(request.AllocationRequest.TaskOwner?.Persons?.FirstOrDefault()?.AzureUniqueId)

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
                    .TryAddOpenPortalUrlAction("Open request", $"{request.PersonnelAllocationPortalUrl}")
                    .TryAddOpenPortalUrlAction("Open position in org chart", $"{request.OrgPortalUrl}");

                    var card = await notificationBuilder.BuildCardAsync();
                    await mediator.Send(new NotifyResourceOwner(request.AllocationRequest.AssignedDepartment, card));

                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                }
                //var jsonRep = card.ToJson(); // Json can be viewed using https://adaptivecards.io/designer/
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
                public string? PersonnelAllocationPortalUrl { get; private set; }

                public NotificationRequestData WithContextId(string? contextId)
                {
                    OrgContextId = contextId;
                    return this;
                }

                public NotificationRequestData WithPortalActionUrls()
                {
                    if (!string.IsNullOrEmpty(OrgContextId))
                    {
                        //OrgPortalUrl = $"/apps/org-admin/{OrgContextId}/timeline?instanceId={Instance.Id}&positionId={Position.Id}";
                        OrgPortalUrl = $"aka/goto-org/{OrgContextId}/{Position.Id}/{Instance.Id}";
                    }

                    //PersonnelAllocationPortalUrl = $"/apps/personnel-allocation/my-requests/resource/request/{AllocationRequest.RequestId}";
                    PersonnelAllocationPortalUrl = $"aka/goto-preq/{AllocationRequest.RequestId}";
                    return this;
                }
            }
        }
    }
}