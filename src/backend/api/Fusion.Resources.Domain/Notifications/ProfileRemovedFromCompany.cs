using MediatR;
using System;

namespace Fusion.Resources.Domain.Notifications
{
    public class ProfileRemovedFromCompany : INotification
    {
        public ProfileRemovedFromCompany(Guid azureUniqueId)
        {
            AzureUniqueId = azureUniqueId;
        }

        public Guid AzureUniqueId { get; }
    }
}
