using System;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.ApiClients
{
    public interface IContextApiClient
    {
        Task<Guid?> ResolveContextIdByExternalId(string externalId, string contextType = null);
    }
}