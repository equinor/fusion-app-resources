using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion.Services.LineOrg.ApiModels;

namespace Fusion.Resources.Domain
{
    public interface IOrgUnitCache
    {
        Task<IEnumerable<ApiOrgUnit>> GetOrgUnitsAsync();
        Task ClearOrgUnitCacheAsync();
    }
}
