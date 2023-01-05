using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion.Resources.Domain;
using Fusion.Services.LineOrg.ApiModels;

namespace Fusion.Resources.Api.Tests;

public class OrgUnitCacheMock : IOrgUnitCache
{
    public async Task<IEnumerable<ApiOrgUnit>> GetOrgUnitsAsync()
    {
        var orgUnits = new List<ApiOrgUnit>();
        return orgUnits;
    }

    public Task ClearOrgUnitCacheAsync()
    {
        return Task.CompletedTask;
    }
}