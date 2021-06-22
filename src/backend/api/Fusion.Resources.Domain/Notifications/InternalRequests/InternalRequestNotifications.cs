using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.ApiClients.Org;
using Fusion.Integration;
using Fusion.Integration.Notification;
using Fusion.Integration.Org;
using Fusion.Resources.Domain.Commands;
using Fusion.Resources.Domain.Queries;

namespace Fusion.Resources.Domain.Notifications.InternalRequests
{
    public partial class InternalRequestNotifications
    {
        public class AssignedDepartment : INotification
        {
            public AssignedDepartment(Guid requestId, string assignedDepartment)
            {
                RequestId = requestId;
                Department = assignedDepartment;
            }

            public Guid RequestId { get; }
            public string Department { get; }
        }

        public class InternalRequestAssignedDepartmentHandler : INotificationHandler<AssignedDepartment>
        {
            private readonly IMediator mediator;
            private readonly INotificationBuilder notificationBuilder;
            private readonly IProjectOrgResolver orgResolver;
            private readonly IFusionContextResolver contextResolver;

            public InternalRequestAssignedDepartmentHandler(IMediator mediator, INotificationBuilderFactory notificationClient, IProjectOrgResolver orgResolver, IFusionContextResolver contextResolver)
            {
                this.mediator = mediator;
                this.notificationBuilder = notificationClient.CreateDesigner();
                this.orgResolver = orgResolver;
                this.contextResolver = contextResolver;
            }


            public async Task Handle(AssignedDepartment notification,
                CancellationToken cancellationToken)
            {
                var request = await GetResolvedOrgData(notification.RequestId);


                notificationBuilder.TryAddProfileCard(request.AllocationRequest.CreatedBy.AzureUniqueId)
                    //.TryAddProfileCard("TASK OWNER")
                    //.TryAddProfileCard("POSITION REPORTS TO")
                    //.TryAddProfileCard(request.Instance.AssignedPerson.AzureUniqueId)
                    .AddDescription("Please review and handle request")
                    .AddFacts(facts => facts
                        .AddFact("Project", request.Position.Project.Name)
                        .AddFact("Position", request.Position.Name)
                        .AddFact("Period", $"{request.Instance.AppliesFrom:dd.MM.yyyy} - {request.Instance.AppliesTo:dd.MM.yyyy}")
                        .AddFact("Workload", $"{request.Instance.Workload}")
                    )
                    .TryAddOpenPortalUrlAction("Open request", $"{request.PersonnelAllocationPortalUrl}")
                    .TryAddOpenPortalUrlAction("Open position in org chart", $"{request.OrgAdminPortalUrl}")
                    ;

                var card = await notificationBuilder.BuildCardAsync();
                await mediator.Send(new NotifyResourceOwner(notification.RequestId, card));
            }

            private async Task<NotificationRequestData> GetResolvedOrgData(Guid requestId)
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
                    .WithPortalActions();
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
                public string? OrgAdminPortalUrl { get; private set; }
                public string? PersonnelAllocationPortalUrl { get; private set; }


                public NotificationRequestData WithContextId(string? contextId)
                {
                    OrgContextId = contextId;
                    return this;
                }

                public NotificationRequestData WithPortalActions()
                {
                    if (!string.IsNullOrEmpty(OrgContextId))
                    {
                        OrgAdminPortalUrl = $"/apps/org-admin/{OrgContextId}/timeline?instanceId={Instance.Id}&positionId={Position.Id}";
                    }

                    PersonnelAllocationPortalUrl = $"/apps/personnel-allocation/my-requests/resource/request/{AllocationRequest.RequestId}";
                    return this;
                }
            }
        }
    }
}