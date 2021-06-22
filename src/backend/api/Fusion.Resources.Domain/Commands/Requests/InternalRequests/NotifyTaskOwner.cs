using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Fusion.Integration.Notification;
using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Resources.Domain.Commands
{
    public class NotifyTaskOwner : TrackableRequest
    {
        public NotifyTaskOwner(Guid requestId, AdaptiveCard card)
        {
            this.RequestId = requestId;
            this.Card = card;
        }

        public Guid RequestId { get; }
        public AdaptiveCard Card { get; }

    }

    public class Handler : AsyncRequestHandler<NotifyTaskOwner>
    {
        private readonly IFusionNotificationClient notificationClient;
        private readonly IMediator mediator;
        private readonly ResourcesDbContext dbContext;

        public Handler(IFusionNotificationClient notificationClient, IMediator mediator, ResourcesDbContext dbContext)
        {
            this.notificationClient = notificationClient;
            this.mediator = mediator;
            this.dbContext = dbContext;
        }
        protected override async Task Handle(NotifyTaskOwner request, CancellationToken cancellationToken)
        {
            var person = await dbContext.Persons.FirstOrDefaultAsync(p => p.Id == request.Editor.Person.Id);
            var req = await dbContext.ResourceAllocationRequests.FirstOrDefaultAsync(x => x.Id == request.RequestId);

            var recipients = await GenerateTaskOwnerRecipientsForRequestAsync(person.AzureUniqueId, req.AssignedDepartment);

            var args = new NotificationArguments("Personnel allocation - notification");
            foreach (var recipient in recipients)
            {
                await notificationClient.CreateNotificationForUserAsync(recipient, args, request.Card);
            }
        }

        private async Task<IEnumerable<Guid>> GenerateTaskOwnerRecipientsForRequestAsync(Guid notificationInitiatedByAzureUniqueId, string? assignedDepartment)
        {
            var recipients = new List<Guid>();

            if (!string.IsNullOrEmpty(assignedDepartment))
            {
                var ro = await mediator.Send(new GetDepartment(assignedDepartment).ExpandDelegatedResourceOwners());
                var relevantProfiles = new List<Guid?>();
                if (ro?.LineOrgResponsible?.AzureUniqueId != null)
                    relevantProfiles.Add(ro.LineOrgResponsible.AzureUniqueId);

                if (ro?.DelegatedResourceOwners != null)
                    relevantProfiles.AddRange(ro.DelegatedResourceOwners.Select(x => x.AzureUniqueId));

                recipients.AddRange(from azureUniqueId in relevantProfiles.Where(x => x.HasValue).Distinct()
                                    where azureUniqueId.Value != notificationInitiatedByAzureUniqueId
                                    select azureUniqueId.Value);
            }

            return recipients.Distinct();// A person may be a have multiple roles.
        }
    }
}