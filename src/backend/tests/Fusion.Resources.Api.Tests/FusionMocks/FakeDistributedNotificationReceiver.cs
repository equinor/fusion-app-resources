using Fusion.Infrastructure.MediatR.Distributed;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Tests.FusionMocks
{
    /// <summary>
    /// Fusion.Infrastructure.MediatR.
    /// Mock type for distributed mediatr implementation. 
    /// </summary>
    public class FakeDistributedNotificationReceiver : IDistributedNotificationReceiver
    {
        public Task StartAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
