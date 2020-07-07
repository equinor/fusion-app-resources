using Fusion.Integration;
using Fusion.Integration.Notification;
using Fusion.Resources.Database;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Notifications;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Notifications
{
    public class CreateFusionNotificationHandler : INotificationHandler<ContractRoleDelegated>
    {
        private readonly IMediator mediator;
        private readonly IFusionNotificationClient notificationClient;

        public CreateFusionNotificationHandler(IMediator mediator, IFusionNotificationClient notificationClient)
        {
            this.mediator = mediator;
            this.notificationClient = notificationClient;
        }

        public async Task Handle(ContractRoleDelegated notification, CancellationToken cancellationToken)
        {
            var delegatedRole = await mediator.Send(new Domain.GetContractDelegatedRole(notification.RoleId));

            if (delegatedRole == null)
                return;


            await notificationClient.CreateNotificationAsync(notification => notification
                    .WithRecipient(delegatedRole.Person.AzureUniqueId)
                    .WithTitle(NotificationTitle(delegatedRole))
                    .WithDescriptionMarkdown(NotificationDescription(delegatedRole)));
        }

        private string NotificationTitle(QueryDelegatedRole role) => $"You were delegated {role.Type} role in {role.Contract.ContractNumber} - {role.Project.Name}";
        private string NotificationDescription(QueryDelegatedRole role) => $"{role.CreatedBy.Name} ({role.CreatedBy.Mail}) delegated you {role.Type} role in " +
            $"{role.Contract.ContractNumber} - {role.Project.Name}.";
    }
}
