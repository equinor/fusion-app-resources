using Fusion.Integration;
using Fusion.Integration.Diagnostics;
using Fusion.Resources.Application.LineOrg.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Application.LineOrg
{
    public class LineOrgResolver : ILineOrgResolver
    {
        const int page_size = 500; //max resolved people from people service simultanously = 500.

        private readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1);
        private DepartmentCache cache;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IHostingEnvironment hostingEnvironment;
        private readonly IFusionProfileResolver profileResolver;
        private readonly IFusionLogger<LineOrgResolver> logger;

        public LineOrgResolver(IHttpClientFactory httpClientFactory,
            IHostingEnvironment hostingEnvironment,
            IFusionProfileResolver profileResolver,
            IFusionLogger<LineOrgResolver> logger)
        {
            this.cache = new DepartmentCache();
            this.httpClientFactory = httpClientFactory;
            this.hostingEnvironment = hostingEnvironment;
            this.profileResolver = profileResolver;
            this.logger = logger;
        }

        public async Task<List<LineOrgDepartment>> GetResourceOwners(string? filter, CancellationToken cancellationToken)
        {
            if (!cache.IsValid)
            {
                await RehydrateCache();
            }

            var departments = cache.Search(filter);
            if (!departments.Any())
            {
                await UpdateCacheItems(filter);
                departments = cache.Search(filter);
            }

            var profiles = new List<Integration.Profile.ResolvedPersonProfile>();
            for (int i = 0; i < departments.Count(); i += page_size)
            {
                profiles.AddRange(await profileResolver.ResolvePersonsAsync(
                    departments
                        .Skip(i * page_size)
                        .Take(Math.Min(page_size, departments.Count() - page_size * i))
                        .Select(r => new Integration.Profile.PersonIdentifier(r.LineOrgResponsibleId))
                ));
            }

            var resolvedProfiles = profiles
                .Where(p => p.Success)
                .ToDictionary(p => p.Profile!.AzureUniqueId!.Value);

            var result = new List<LineOrgDepartment>();

            foreach (var department in departments)
            {
                if (!resolvedProfiles.ContainsKey(department.LineOrgResponsibleId)) continue;

                result.Add(new LineOrgDepartment(department.DepartmentId)
                {
                    Responsible = resolvedProfiles[department.LineOrgResponsibleId]?.Profile
                });
            }
            return result;
        }

        private async Task RehydrateCache()
        {
            await semaphoreSlim.WaitAsync();

            if (cache.IsValid) return; // rehydration completed on other thread

            var rebuiltCache = new DepartmentCache();

            try
            {
                await UpdateCacheItemsUnsafe(rebuiltCache);

                cache = rebuiltCache;
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        private async Task UpdateCacheItems(string? filter = null)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                await UpdateCacheItemsUnsafe(cache, filter);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        private async Task UpdateCacheItemsUnsafe(DepartmentCache cache, string? filter = null)
        {
            var client = httpClientFactory.CreateClient("lineorg");
            var uri = $"/lineorg/persons?$top={page_size}&$filter=isresourceowner eq true";
            if (!string.IsNullOrEmpty(filter))
            {
                uri += $"&$search={Uri.EscapeDataString(filter)}";
            }

            do
            {
                var response = await client.GetAsync(uri);
                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    logger.Log(LogLevel.Critical, "Unable to read department info from line org.");
                    return;
                }

                var page = JsonSerializer.Deserialize<PaginatedResponse<ProfileWithDepartment>>(
                    await response.Content.ReadAsStringAsync(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                uri = page!.NextPage;
                var resourceOwners = page!.Value!;

                var profiles = await profileResolver.ResolvePersonsAsync(
                    resourceOwners.Select(r => new Integration.Profile.PersonIdentifier(r.AzureUniqueId))
                );

                var resolvedProfiles = profiles
                    .Where(p => p.Success && p.Profile!.AzureUniqueId.HasValue)
                    .ToDictionary(p => p.Profile!.AzureUniqueId!.Value);

                foreach (var resourceOwner in resourceOwners)
                {
                    if (!resolvedProfiles.ContainsKey(resourceOwner.AzureUniqueId)) continue;

                    var profile = resolvedProfiles[resourceOwner.AzureUniqueId].Profile!;
                    var shortname = profile.Mail?.Contains('@') == true ? profile.Mail.Split("@")[0] : "";
                    var searchText = $"{shortname}|{profile.Name}|{resourceOwner.FullDepartment}";

                    var cacheItem = new DepartmentCacheItem(resourceOwner!.FullDepartment, searchText, resourceOwner.AzureUniqueId);
                    if (cache.Contains(cacheItem.DepartmentId))
                    {
                        cache.Remove(cacheItem.DepartmentId);
                    }

                    cache.Add(cacheItem);
                }
            } while (!string.IsNullOrEmpty(uri));
        }
    }
}
