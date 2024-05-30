using Fusion.Events;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Domain;
using Microsoft.Extensions.Caching.Memory;

namespace Fusion.Resources.Api
{
    /// <summary>
    /// Handler listening on Line-Org service changes to org-units.
    /// </summary>
    public class LineOrgOrgUnitHandler : ISubscriptionHandler
    {
        private readonly IOrgUnitCache orgUnitCache;
        private readonly IMemoryCache cache;

        public LineOrgOrgUnitHandler(IOrgUnitCache orgUnitCache, IMemoryCache cache)
        {
            this.orgUnitCache = orgUnitCache;
            this.cache = cache;
        }

        public async Task ProcessMessageAsync(MessageContext ctx, string? body, CancellationToken cancellationToken)
        {
            await orgUnitCache.ClearOrgUnitCacheAsync();
            cache.Remove(Domain.GetDepartments.Handler.OrgUnitsMemCacheKey);
        }
    }
}