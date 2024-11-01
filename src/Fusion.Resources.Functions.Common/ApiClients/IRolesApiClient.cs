using Fusion.Resources.Functions.Common.ApiClients.ApiModels;

namespace Fusion.Resources.Functions.Common.ApiClients;

public interface IRolesApiClient
{
    public Task<Dictionary<Guid, ICollection<ApiSinglePersonRole>>> GetAdminRolesForOrgProjects(ICollection<Guid> projectIds, CancellationToken cancellationToken = default);
}