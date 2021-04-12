using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Application.LineOrg
{
    public sealed class LineOrgCacheRefresher : IHostedService, IDisposable
    {
        private Timer timer;
        private readonly ILineOrgResolver lineOrgResolver;

        public LineOrgCacheRefresher(ILineOrgResolver lineOrgResolver)
        {
            this.lineOrgResolver = lineOrgResolver;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            timer = new Timer(async _ =>
            {
                await lineOrgResolver.GetResourceOwners(filter: "", cancellationToken);
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
