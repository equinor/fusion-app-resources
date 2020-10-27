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

            return url is null ? null : $"{url}/active-request";
        }

        private string? Host => $"{contextAccessor.HttpContext?.Request?.Scheme}:{contextAccessor.HttpContext?.Request?.Host.ToString()}";

        private async Task<string?> GetResourcesBaseAsync(Guid orgProjectId, Guid orgContractId)
        {
            var context = await contextResolver.ResolveContextAsync(ContextIdentifier.FromExternalId(orgProjectId), FusionContextType.OrgChart);

            if (Host is null || context is null)
                return null;

            return $"{Host}/apps/resources/{context.Id}/{orgContractId}";
        }
    }
}
