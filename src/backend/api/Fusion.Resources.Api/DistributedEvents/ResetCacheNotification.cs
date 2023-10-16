using Fusion.Infrastructure.MediatR.Distributed;
using Fusion.Integration.Profile.Internal;
using Fusion.Resources.Domain;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.DistributedEvents
{
    /// <summary>
    /// Trigger a reset of all internal caches.
    /// </summary>
    public class ResetCacheNotification : DistributedNotification
    {

        public class Handler : INotificationHandler<ResetCacheNotification>
        {
            private readonly IOrgUnitCache orgUnitCache;
            private readonly IProfileCache fusionProfileResolverCache;

            public Handler(IOrgUnitCache orgUnitCache, IProfileCache fusionProfileResolverCache)
            {
                this.orgUnitCache = orgUnitCache;
                this.fusionProfileResolverCache = fusionProfileResolverCache;
            }

            public async Task Handle(ResetCacheNotification notification, CancellationToken cancellationToken)
            {
                await fusionProfileResolverCache.ClearAsync();
                await orgUnitCache.ClearOrgUnitCacheAsync();

            }
        }
    }
}
