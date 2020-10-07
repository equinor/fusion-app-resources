using Fusion.Integration.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Tests.FusionMocks
{
    internal class NotificationClientMock : IFusionNotificationClient
    {
        public Task<IEnumerable<FusionNotification>> CreateNotificationAsync(Action<FusionNotificationBuilder> notificationBuilder)
        {
            var items = new List<FusionNotification>();
            return Task.FromResult(items.AsEnumerable());
        }

        public Task<IEnumerable<FusionNotification>> CreateNotificationAsync(INotificationBuilder notificationFactory)
        {
            var items = new List<FusionNotification>();
            return Task.FromResult(items.AsEnumerable());
        }
    }
}
