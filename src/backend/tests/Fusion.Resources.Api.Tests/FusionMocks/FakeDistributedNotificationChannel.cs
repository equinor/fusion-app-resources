using Fusion.Infrastructure.MediatR.Distributed;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Tests.FusionMocks
{
    /// <summary>
    /// Fusion.Infrastructure.MediatR.
    /// Mock type for distributed mediatr implementation. 
    /// </summary>
    public class FakeDistributedNotificationChannel : IDistributedNotificationChannel
    {
        public List<object> Notifications = new List<object>();

        public Task Publish<T>(T notification) where T : IDistributedNotification
        {
            Notifications.Add(notification);

            return Task.CompletedTask;
        }
    }
}
