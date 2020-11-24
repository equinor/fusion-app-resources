using System;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.ApiClients
{
    public interface IContextApiClient
    {
        Task<Guid?> ResolveContextIdByExternalIdAsync(string externalId, string contextType = null);
    }
}