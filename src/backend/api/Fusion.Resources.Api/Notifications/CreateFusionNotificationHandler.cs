using Fusion.Integration;
using Fusion.Integration.Notification;
using Fusion.Resources.Api.Notifications.Markdown;
using Fusion.Resources.Database;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Notifications;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Notifications
{
    public class CreateFusionNotificationHandler : 
        INotificationHandler<ContractRoleDelegated>,
        INotificationHandler<DelegatedContractRoleRecertified>
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
                    .WithTitle($"You were delegated {delegatedRole.Type} role in {delegatedRole.Contract.ContractNumber} - {delegatedRole.Project.Name}")
                    .WithDescriptionMarkdown(NotificationDescription(delegatedRole)));
        }

        public async Task Handle(DelegatedContractRoleRecertified notification, CancellationToken cancellationToken)
        {
            var delegatedRole = await mediator.Send(new Domain.GetContractDelegatedRole(notification.RoleId));

            if (delegatedRole == null)
                return;


            await notificationClient.CreateNotificationAsync(notification => notification
                    .WithRecipient(delegatedRole.Person.AzureUniqueId)
                    .WithTitle($"Your delegated role in {delegatedRole.Contract.ContractNumber} - {delegatedRole.Project.Name} was recertified")
                    .WithDescriptionMarkdown(NotificationRecertifiedDescription(delegatedRole)));
        }

        private string NotificationRecertifiedDescription(QueryDelegatedRole role) => new MarkdownDocument()
            .Paragraph($"{MdToken.Bold(role.RecertifiedBy?.Name)} ({role.RecertifiedBy?.Mail}) recertified your {role.Type} role in the contract '{role.Contract.Name}'.")
            .List(l => l
                .ListItem($"{MdToken.Bold("Project:")} {role.Project.Name}")
                .ListItem($"{MdToken.Bold("Contract:")} {role.Contract.ContractNumber}"))
            .Paragraph($"The role is **now valid** to **{role.ValidTo:dd/MM yyyy}**.")
            .Build();

        private string NotificationDescription(QueryDelegatedRole role) => new MarkdownDocument()
            .Paragraph($"{MdToken.Bold(role.CreatedBy.Name)} ({role.CreatedBy.Mail}) delegated you the {role.Type} role in the contract '{role.Contract.Name}'.")
            .List(l => l
                .ListItem($"{MdToken.Bold("Project:")} {role.Project.Name}")
                .ListItem($"{MdToken.Bold("Contract:")} {role.Contract.ContractNumber}"))
            .Paragraph($"The role is valid to **{role.ValidTo:dd/MM yyyy}**.")
            .Build();
            
    }
}
