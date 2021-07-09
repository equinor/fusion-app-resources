using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.ApiClients.Org;
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
            private readonly ILogger<InternalRequestNotification> logger;

            public AssignedDepartmentHandler(IMediator mediator, INotificationBuilderFactory notificationBuilderFactory, IProjectOrgResolver orgResolver, ILogger<InternalRequestNotification> logger)
            {
                this.mediator = mediator;
                this.notificationBuilder = notificationBuilderFactory.CreateDesigner();
                this.orgResolver = orgResolver;
                this.logger = logger;
            }

            public async Task Handle(InternalRequestNotifications.AssignedDepartment notification, CancellationToken cancellationToken)
            {
                var request = await GetResolvedOrgDataAsync(notification.RequestId);

                if (string.IsNullOrEmpty(request.AllocationRequest.AssignedDepartment))
                    return;

                var tr = ExtractTaskOwnerFromRequest(request.AllocationRequest);

                try
                {
                    notificationBuilder.AddTitle($"A {request.AllocationRequest.SubType} personnel request has been assigned to your department")
                    .AddTextBlockIf("Task owner for request:", tr.HasTaskOwner)
                    .TryAddProfileCard(tr.MainTaskOwner)

                    .AddTextBlockIf("Proposed resource:", request.Instance.AssignedPerson != null)
                    .TryAddProfileCard(request.Instance.AssignedPerson?.AzureUniqueId)

                    .AddDescription("Please review and handle request")

                    .AddFacts(facts => facts
                        .AddFactIf("Request number", $"{request.AllocationRequest?.RequestNumber}", request.AllocationRequest?.RequestNumber is not null)
                        .AddFactIf("Project", request.Position?.Project?.Name ?? "", request.Position?.Project is not null)
                        .AddFactIf("Position id", request.Position?.ExternalId ?? "", request.Position?.ExternalId is not null)
                        .AddFactIf("Position", request.Position?.Name ?? "", request.Position?.Name is not null)
                        .AddFact("Period", $"{request.Instance.GetFormattedPeriodString()}")
                        .AddFact("Workload", $"{request.Instance?.GetFormattedWorkloadString()}")
                        )
                    .AddTextBlockIf($"Additional task owners: {tr.AdditionalTaskOwnerString}", tr.HasMultipleTaskOwners)
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
            }

            private static TaskOwnerResult ExtractTaskOwnerFromRequest(QueryResourceAllocationRequest allocationRequest)
            {
                var hasTaskOwner = allocationRequest.TaskOwner?.Persons?.Any() ?? false;
                var multipleTaskOwners = allocationRequest.TaskOwner?.Persons?.Length > 1;
                var mainTaskOwner = allocationRequest.TaskOwner?.Persons?.FirstOrDefault()?.AzureUniqueId;
                var additionalTaskOwners = new List<string>();
                additionalTaskOwners.AddRange(allocationRequest.TaskOwner?.Persons?.Skip(1).Select(x => x.Name) ?? Array.Empty<string>());

                var res = new TaskOwnerResult
                {
                    HasTaskOwner = hasTaskOwner,
                    HasMultipleTaskOwners = multipleTaskOwners,
                    MainTaskOwner = mainTaskOwner,
                    AdditionalTaskOwners = additionalTaskOwners

                };
                return res;
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

                return new NotificationRequestData(internalRequest, orgPosition, orgPositionInstance)
                    .WithProjectId($"{internalRequest.Project.OrgProjectId}")
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

                private string? ProjectIdentifier { get; set; }
                public QueryResourceAllocationRequest AllocationRequest { get; }
                public ApiPositionV2 Position { get; }
                public ApiPositionInstanceV2 Instance { get; }
                public string? OrgPortalUrl { get; private set; }
                public string? PersonnelAllocationPortalUrl { get; private set; }

                public NotificationRequestData WithProjectId(string? projectIdentifier)
                {
                    ProjectIdentifier = projectIdentifier;
                    return this;
                }

                public NotificationRequestData WithPortalActionUrls()
                {
                    if (!string.IsNullOrEmpty(ProjectIdentifier))
                    {
                        OrgPortalUrl = $"aka/goto-org/{ProjectIdentifier}/{Position.Id}/{Instance.Id}";
                    }

                    PersonnelAllocationPortalUrl = $"aka/goto-preq/{AllocationRequest.RequestId}";
                    return this;
                }
            }
        }
    }

    internal class TaskOwnerResult
    {
        public bool HasTaskOwner { get; set; }
        public bool HasMultipleTaskOwners { get; set; }
        public Guid? MainTaskOwner { get; set; }
        public List<string> AdditionalTaskOwners { get; set; } = new();
        public string AdditionalTaskOwnerString => string.Join(",", AdditionalTaskOwners.OrderBy(x => x));
    }
}