using Fusion.Resources.Functions.Common.ApiClients.ApiModels;

namespace Fusion.Resources.Functions.Common.ApiClients;

public interface IRolesApiClient
{
    public Task<Dictionary<Guid, ICollection<ApiSinglePersonRole>>> GetAdminRolesForOrgProjects(IEnumerable<Guid> projectIds);

    public Task<Dictionary<Guid, ICollection<ApiSinglePersonRole>>> GetExpiringAdminRolesForOrgProjects(IEnumerable<Guid> projectIds, int monthsUntilExpiry);
}