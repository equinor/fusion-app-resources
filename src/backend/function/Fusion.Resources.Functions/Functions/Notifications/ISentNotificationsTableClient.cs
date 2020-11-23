using Fusion.Resources.Functions.ApiClients;
using System;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.Functions.Notifications
{
    public interface ISentNotificationsTableClient
    {
        Task AddToSentNotifications(Guid requestId, Guid recipientId, string state);

        Task<SentNotification> GetSentNotificationsAsync(Guid requestId, Guid recipientId, string state);

        Task<bool> NotificationWasSentAsync(Guid requestId, Guid recipientId, string state);
    }
}