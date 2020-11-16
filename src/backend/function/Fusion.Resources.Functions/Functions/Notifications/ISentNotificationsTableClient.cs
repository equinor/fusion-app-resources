using System;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.Functions.Notifications
{
    public interface ISentNotificationsTableClient
    {
        Task AddToSentNotifications(Guid requestId, Guid recipientId);
        Task<SentNotifications> GetSentNotificationsAsync(Guid requestId, Guid recipientId);
        Task<bool> NotificationWasSentAsync(Guid requestId, Guid recipientId);
    }
}