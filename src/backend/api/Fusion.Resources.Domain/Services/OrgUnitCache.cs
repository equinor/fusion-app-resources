using Fusion.Integration;
using Fusion.Services.LineOrg.ApiModels;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Services
{
    internal class OrgUnitCache : IOrgUnitCache
    {
        private readonly IMemoryCache memoryCache;
        private readonly HttpClient lineOrgClient;
        const string CacheKey = "OrgUnitCache.GetOrgUnitsAsync";

        private static readonly SemaphoreSlim Locker = new(1);

        public OrgUnitCache(IHttpClientFactory httpClientFactory, IMemoryCache memoryCache)
        {
            this.memoryCache = memoryCache;

            this.lineOrgClient = httpClientFactory.CreateClient(IntegrationConfig.HttpClients.ApplicationLineOrg());





        }

        public async Task<IEnumerable<ApiOrgUnit>> GetOrgUnitsAsync()
        {
            await Locker.WaitAsync();
            try
            {
                var data = await memoryCache.GetOrCreateAsync(CacheKey, async (_) =>
                {
                    var orgUnitResponse =
                        await lineOrgClient.GetFromJsonAsync<ApiPagedCollection<ApiOrgUnit>>("/org-units?$top=50000");
                    if (orgUnitResponse is null)
                        throw new InvalidOperationException("Could not fetch org units from line org");

                    return orgUnitResponse.Value;
                });
                return data;
            }
            finally
            {
                Locker.Release();
            }
        }
        public Task ClearOrgUnitCacheAsync()
        {
            memoryCache.Remove(CacheKey);
            return Task.CompletedTask;
        }
    }
}