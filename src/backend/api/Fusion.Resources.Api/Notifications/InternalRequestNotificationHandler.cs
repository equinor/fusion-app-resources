using Fusion.Integration.Notification;
using Fusion.Integration.Org;
using Fusion.Resources.Api.Notifications.Markdown;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Notifications.Request;
using Fusion.Resources.Domain.Queries;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Notifications
{
    public class InternalRequestNotificationHandler :
        INotificationHandler<InternalJointVentureRequestProposal>
    {
        private readonly IMediator mediator;
        private readonly IFusionNotificationClient notificationClient;
        private readonly IProjectOrgResolver orgResolver;

        public InternalRequestNotificationHandler(IMediator mediator, IFusionNotificationClient notificationClient, IProjectOrgResolver orgResolver)
        {
            this.mediator = mediator;
            this.notificationClient = notificationClient;
            this.orgResolver = orgResolver;
        }
        public async Task Handle(InternalJointVentureRequestProposal notification, CancellationToken cancellationToken)
        {
            var request = (await GetRequestAsync(notification.RequestId))!;
            var recipients = await GetPositionInstanceTaskOwnersAsync(request);

            foreach (var recipient in recipients)
            {
                await notificationClient.CreateNotificationForUserAsync(recipient, $"Requesting personnel for position: {request.OrgPosition!.Name}", builder =>
                {
                    builder.AddDescription(NotificationDescription.RequestPersonForPositionAsync(request, $"{request.RequestId}"));
                });
            }
        }

        private async Task<QueryResourceAllocationRequest?> GetRequestAsync(Guid requestId)
        {
            var query = new GetResourceAllocationRequestItem(requestId);
            var request = await mediator.Send(query);

            return request;
        }

        private async Task<List<Guid>> GetPositionInstanceTaskOwnersAsync(QueryResourceAllocationRequest request)
        {
            var position = await orgResolver.ResolvePositionAsync(request.OrgPosition!.Id);

            if (position == null)
                throw new InvalidOperationException($"Cannot resolve position for request {request.RequestId}");

            var instanceTaskOwners = position.Instances.FirstOrDefault(x => x.Id == request.OrgPositionInstance!.Id)?.TaskOwnerIds;
            return instanceTaskOwners ?? new List<Guid>();
        }

        private static class NotificationDescription
        {
            public static string RequestPersonForPositionAsync(QueryResourceAllocationRequest request, string? activeRequestsUrl) => new MarkdownDocument()
                .Paragraph($"Request was created by {request.CreatedBy?.Name} ({request.CreatedBy?.Mail}).")
                .Paragraph($"Please review and follow up request in Resource Allocation")
                .List(l => l
                    .ListItem($"{MdToken.Bold("Project:")} {request.Project?.Name}")
                    .ListItem($"{MdToken.Bold("Position:")} {request.OrgPosition?.Name}"))
                .LinkParagraph("Open active request", activeRequestsUrl)
                .Build();

        }
    }
}
