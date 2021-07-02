using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Integration.Notification;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands;
using MediatR;

namespace Fusion.Resources.Api.Notifications
{
    public partial class InternalRequestNotification
    {
        public class NotifyResourceOwnerHandler : AsyncRequestHandler<NotifyResourceOwner>
        {
            private readonly IFusionNotificationClient notificationClient;
            private readonly IMediator mediator;

            public NotifyResourceOwnerHandler(IFusionNotificationClient notificationClient, IMediator mediator)
            {
                this.notificationClient = notificationClient;
                this.mediator = mediator;
            }
            protected override async Task Handle(NotifyResourceOwner request, CancellationToken cancellationToken)
            {
                var recipients = await GenerateRecipientsAsync(request.Editor.Person.AzureUniqueId, request.AssignedDepartment);
                
                var arguments = new NotificationArguments($"A personnel request has been assigned to you (you are notified as resource owner)") { AppKey = "personnel-allocation" };
                foreach (var recipient in recipients)
                {
                    await notificationClient.CreateNotificationForUserAsync(recipient, arguments, request.Card);
                }
            }

            private async Task<IEnumerable<Guid>> GenerateRecipientsAsync(Guid notificationInitiatedByAzureUniqueId, string? assignedDepartment)
            {
                var recipients = new List<Guid>();

                if (string.IsNullOrEmpty(assignedDepartment))
                    return recipients;

                var ro = await mediator.Send(new GetDepartment(assignedDepartment).ExpandDelegatedResourceOwners());
                var relevantProfiles = new List<Guid?>();
                if (ro?.LineOrgResponsible?.AzureUniqueId != null)
                    relevantProfiles.Add(ro.LineOrgResponsible.AzureUniqueId);

                if (ro?.DelegatedResourceOwners != null)
                    relevantProfiles.AddRange(ro.DelegatedResourceOwners.Select(x => x.AzureUniqueId));

                recipients.AddRange(from azureUniqueId in relevantProfiles.Where(x => x.HasValue).Distinct()
                                    where azureUniqueId.Value != notificationInitiatedByAzureUniqueId
                                    select azureUniqueId.Value);

                return recipients.Distinct();// A person may be a have multiple roles.
            }
        }
    }
}