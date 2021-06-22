using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Fusion.Integration.Notification;
using MediatR;

namespace Fusion.Resources.Domain.Commands
{
    public class NotifyResourceOwner : TrackableRequest
    {
        public NotifyResourceOwner(string assignedDepartment, AdaptiveCard card)
        {
            this.AssignedDepartment = assignedDepartment;
            this.Card = card;
        }

        public string AssignedDepartment{ get; }
        public AdaptiveCard Card { get; }

    }

    public class Handler : AsyncRequestHandler<NotifyResourceOwner>
    {
        private readonly IFusionNotificationClient notificationClient;
        private readonly IMediator mediator;

        public Handler(IFusionNotificationClient notificationClient, IMediator mediator)
        {
            this.notificationClient = notificationClient;
            this.mediator = mediator;
        }
        protected override async Task Handle(NotifyResourceOwner request, CancellationToken cancellationToken)
        {
            var recipients = await GenerateRecipientsAsync(request.Editor.Person.AzureUniqueId, request.AssignedDepartment);
            
            foreach (var recipient in recipients)
            {
                await notificationClient.CreateNotificationForUserAsync(recipient, "A personnel request has been assigned to you", request.Card);
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