using Fusion.Events;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Domain;

namespace Fusion.Resources.Api
{
    /// <summary>
    /// Handler listening on Line-Org service changes to org-units.
    /// </summary>
    public class LineOrgOrgUnitChangedEvent : ISubscriptionHandler
    {
        private readonly IOrgUnitCache orgUnitCache;


        public LineOrgOrgUnitChangedEvent(IOrgUnitCache orgUnitCache)
        {
            this.orgUnitCache = orgUnitCache;
        }

        public async Task ProcessMessageAsync(MessageContext ctx, string body, CancellationToken cancellationToken)
        {
            await orgUnitCache.ClearOrgUnitCacheAsync();
        }
    }
}
