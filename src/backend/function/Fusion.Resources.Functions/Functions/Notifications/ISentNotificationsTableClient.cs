using System;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.Notifications
{
    public interface ISentNotificationsTableClient
    {
        Task AddToSentNotifications(Guid requestId, Guid recipientId, string state);
        Task CleanupSentNotifications(DateTime dateCutoff);
        Task<SentNotification> GetSentNotificationsAsync(Guid requestId, Guid recipientId, string state);

        Task<bool> NotificationWasSentAsync(Guid requestId, Guid recipientId, string state);
    }
}