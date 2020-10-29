using Fusion.Integration;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Notifications
{
    public class UrlResolver : IUrlResolver
    {
        private readonly IHttpContextAccessor contextAccessor;
        private readonly IFusionContextResolver contextResolver;

        public UrlResolver(IHttpContextAccessor contextAccessor, IFusionContextResolver contextResolver)
        {
            this.contextAccessor = contextAccessor;
            this.contextResolver = contextResolver;
        }

        public async Task<string?> ResolveManagePersonnel(Guid orgProjectId, Guid orgContractId)
        {
            string? url = await GetResourcesBaseAsync(orgProjectId, orgContractId);

            return url is null ? null : $"{url}/manage-personnel";
        }

        public async Task<string?> ResolveActiveRequests(Guid orgProjectId, Guid orgContractId)
        {
            string? url = await GetResourcesBaseAsync(orgProjectId, orgContractId);

            return url is null ? null : $"{url}/active-requests";
        }

        private async Task<string?> GetResourcesBaseAsync(Guid orgProjectId, Guid orgContractId)
        {
            var referer = contextAccessor.HttpContext?.Request?.GetTypedHeaders().Referer;
            var context = await contextResolver.ResolveContextAsync(ContextIdentifier.FromExternalId(orgProjectId), FusionContextType.OrgChart);

            if (referer is null || context is null)
                return null;

            //Referer.Authority will contain both dns and port
            return $"{referer.Scheme}://{referer.Authority}/apps/resources/{context.Id}/{orgContractId}";
        }
    }
}
