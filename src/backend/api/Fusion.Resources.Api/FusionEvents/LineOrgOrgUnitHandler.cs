using Fusion.Events;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Domain;
using Microsoft.Extensions.Caching.Memory;
using Fusion.Resources.Application.LineOrg;
using Fusion.Resources.Api.Controllers;
using Newtonsoft.Json;

namespace Fusion.Resources.Api
{

    /// <summary>
    /// Handler listening on Line-Org service changes to org-units.
    /// 
    /// Transient handler which should be executed in all instances of the api. 
    /// Primarily used for invalidating and clearing cache. 
    /// 
    /// Operations that modify data should be run in the persistant handler.
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
            cache.Remove(LineOrgClient.OrgUnitsMemCacheKey);

            // Process the even 
            switch (ctx.Event.Type)
            {
                case var org when org == LineOrgEventTypes.OrgUnit.Name:
                    await HandleOrgUnitEventAsync(body);
                    break;
            }
        }

        private Task HandleOrgUnitEventAsync(string? body)
        {   
            var payloadData = JsonConvert.DeserializeObject<LineOrgEventBody>(body ?? "");

            if (payloadData is null)
            {
                return Task.CompletedTask;
            }

            if (payloadData.GetChangeType() == LineOrgEventBody.ChangeType.Updated)
            {
                OrgUnitResolver.ClearCache(payloadData.SapId);
                OrgUnitResolver.ClearCache(payloadData.FullDepartment);
            }

            return Task.CompletedTask;
        }

        /* Example payload
        * {
        *  "SapId":"53079987",                      // Sap id, should not change
        *  "FullDepartment":"EPI DEV CADS BRA",     // The fullDepartment, pre update
        *  "Type":"Updated",
        *  "Changes":["Name","ShortName","Department","FullDepartment","ParentSapId","Parents"]
        * }
        */
    }
}