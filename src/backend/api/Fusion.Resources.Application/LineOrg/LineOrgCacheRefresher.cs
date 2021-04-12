using Fusion.Integration.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Application.LineOrg
{
    public sealed class LineOrgCacheRefresher : IHostedService, IDisposable
    {
        private Timer timer;
        private readonly ILineOrgResolver lineOrgResolver;
        private readonly IFusionLogger logger;

        public LineOrgCacheRefresher(ILineOrgResolver lineOrgResolver, IFusionLogger logger)
        {
            this.lineOrgResolver = lineOrgResolver;
            this.logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            timer = new Timer(async _ =>
            {
                try
                {
                    await lineOrgResolver.GetResourceOwners(filter: "", cancellationToken);
                }
                catch(Exception ex)
                {
                    logger.Log(LogLevel.Error, ex, "Line org integration: Failed to refresh departments cache.");
                }
            }, null, TimeSpan.Zero, TimeSpan.FromDays(1));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose() => timer.Dispose();
    }
}
