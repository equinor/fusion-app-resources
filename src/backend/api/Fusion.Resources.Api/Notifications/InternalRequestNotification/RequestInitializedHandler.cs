using Fusion.Integration.Notification;
using Fusion.Integration.Org;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Queries;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.ApiClients.Org;
using Fusion.Integration;
using Fusion.Resources.Database;
using Fusion.Resources.Domain.Commands;
using Microsoft.Extensions.Logging;

namespace Fusion.Resources.Api.Notifications
{
    public partial class InternalRequestNotification
    {
        public class RequestInitializedHandler : INotificationHandler<Logic.Commands.ResourceAllocationRequest.RequestInitialized>
        {
            private readonly IMediator mediator;
            private readonly INotificationBuilder notificationBuilder;
            private readonly IProjectOrgResolver orgResolver;
            private readonly IFusionContextResolver contextResolver;
            private readonly ResourcesDbContext dbContext;
            private readonly ILogger<InternalRequestNotification> logger;

            public RequestInitializedHandler(IMediator mediator, INotificationBuilderFactory notificationBuilderFactory,
                IProjectOrgResolver orgResolver, IFusionContextResolver contextResolver, ResourcesDbContext dbContext, ILogger<InternalRequestNotification> logger)
            {
                this.mediator = mediator;
                this.notificationBuilder = notificationBuilderFactory.CreateDesigner();
                this.orgResolver = orgResolver;
                this.contextResolver = contextResolver;
                this.dbContext = dbContext;
                this.logger = logger;
            }

            public async Task Handle(Logic.Commands.ResourceAllocationRequest.RequestInitialized notification, CancellationToken cancellationToken)
            {
                var request = await GetResolvedOrgDataAsync(notification.RequestId);
                if (request.AllocationRequest.TaskOwner?.Persons?.Any() == false)
                    return;

                try
                {
                    notificationBuilder
                            .AddTitle("A personnel request has been created")
                            .AddTextBlockIf($"Awaiting feedback from resource owner.", request.Instance?.AssignedPerson is null)
                            .AddTextBlockIf($"Person was proposed.", request.Instance?.AssignedPerson is not null)
                            .TryAddProfileCard(request.Instance?.AssignedPerson?.AzureUniqueId)

                            .AddFacts(facts => facts
                                .AddFactIf("Request number", $"{request.AllocationRequest?.RequestNumber}", request.AllocationRequest?.RequestNumber is not null)
                                .AddFactIf("Project", request.Position?.Project?.Name ?? "", request.Position?.Project is not null)
                                .AddFactIf("Position id", request.Position?.ExternalId ?? "", request.Position?.ExternalId is not null)
                                .AddFactIf("Position", request.Position?.Name ?? "", request.Position?.Name is not null)
                                .AddFact("Period", $"{request.Instance?.AppliesFrom:dd.MM.yyyy} - {request.Instance?.AppliesTo:dd.MM.yyyy}") // Until we have resolved date formatting issue related to timezone.
                                .AddFact("Workload", $"{request.Instance?.Workload}")
                            )
                            .AddTextBlock($"Created by: {request.AllocationRequest.CreatedBy.Name}")
                            .TryAddOpenPortalUrlAction("Open position in org admin", $"{request.OrgAdminUrl}")
                            ;
                    var card = await notificationBuilder.BuildCardAsync();
                    await mediator.Send(new NotifyTaskOwner(request.AllocationRequest.RequestId, card));
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

                var orgPosition =
                    await orgResolver.ResolvePositionAsync(internalRequest.OrgPositionId.GetValueOrDefault());
                if (orgPosition == null)
                    throw new InvalidOperationException($"Cannot resolve position for request {internalRequest.RequestId}");

                var orgPositionInstance =
                    orgPosition.Instances.SingleOrDefault(x => x.Id == internalRequest.OrgPositionInstanceId);
                if (orgPositionInstance == null)
                    throw new InvalidOperationException($"Cannot resolve position instance for request {internalRequest.RequestId}");

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
                public NotificationRequestData(QueryResourceAllocationRequest allocationRequest, ApiPositionV2 position, ApiPositionInstanceV2 instance)
                {
                    AllocationRequest = allocationRequest;
                    Position = position;
                    Instance = instance;
                }

                private string? OrgContextId { get; set; }
                public QueryResourceAllocationRequest AllocationRequest { get; }
                public ApiPositionV2 Position { get; }
                public ApiPositionInstanceV2 Instance { get; }
                public string? OrgAdminUrl { get; private set; }

                public NotificationRequestData WithContextId(string? contextId)
                {
                    OrgContextId = contextId;
                    return this;
                }

                public NotificationRequestData WithPortalActionUrls()
                {
                    if (!string.IsNullOrEmpty(OrgContextId))
                    {
                        OrgAdminUrl = $"aka/goto-org-admin/{OrgContextId}/{Position.Id}/{Instance.Id}";
                    }
                    return this;
                }
            }
        }
    }
}
