using Fusion.Resources.Api.Authorization.Requirements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fusion.Resources.Domain.Services.OrgClient;

namespace Fusion.Resources.Api.Authorization.Handlers
{
    public class OrgProjectAccessHandler : AuthorizationHandler<Requirements.OrgProjectAccessRequirement>
    {
        private readonly IOrgApiClientFactory orgApiClientFactory;
        private readonly IMemoryCache memCache;

        public OrgProjectAccessHandler(IOrgApiClientFactory orgApiClientFactory, IMemoryCache memCache)
        {
            this.orgApiClientFactory = orgApiClientFactory;
            this.memCache = memCache;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, OrgProjectAccessRequirement requirement)
        {
            if (context.User.GetAzureUniqueId() is null)
            {
                requirement.SetEvaluation("No unique object id located, cannot evaluate");
                return;
            }

            var allowedMethods = await QueryAccessAsync(context, requirement);
            
            if (allowedMethods is null)
                return;

            switch (requirement.RequiredLevel)
            {
                case OrgProjectAccessRequirement.AccessLevel.Read:
                    if (allowedMethods.Contains("get", StringComparer.OrdinalIgnoreCase))
                        context.Succeed(requirement);
                    break;

                case OrgProjectAccessRequirement.AccessLevel.Write:
                    if (allowedMethods.Contains("put", StringComparer.OrdinalIgnoreCase))
                        context.Succeed(requirement);
                    break;
            }

        }

        private async Task<IEnumerable<string>?> QueryAccessAsync(AuthorizationHandlerContext context, OrgProjectAccessRequirement requirement)
        {
            string cacheKey = $"project-options-{requirement.OrgProjectId}-{context.User.GetAzureUniqueIdOrThrow()}";
            
            if (memCache.TryGetValue(cacheKey, out string[] cachedHeaders))
            {
                requirement.SetEvaluation("Using cached access info...");
                return cachedHeaders;
            }

            var client = orgApiClientFactory.CreateClient();

            var request = new HttpRequestMessage(new HttpMethod("OPTIONS"), $"/projects/{requirement.OrgProjectId}");
            var response = await client.SendAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                requirement.SetEvaluation("Org service reported no access");
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                requirement.SetEvaluation($"Could not query access from org chart. Service responded with {response.StatusCode}");
                return null;
            }

            var allowedMethods = response.Content.Headers.Allow;

            if (!allowedMethods.Any())
            {
                requirement.SetEvaluation("No allow header located...");
                return null;
            }

            memCache.Set(cacheKey, allowedMethods.ToArray(), TimeSpan.FromMinutes(5));
            return allowedMethods;
        }
    }
}
