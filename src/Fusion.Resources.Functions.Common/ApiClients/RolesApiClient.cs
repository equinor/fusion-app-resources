using Fusion.Resources.Functions.Common.ApiClients.ApiModels;
using Fusion.Resources.Functions.Common.Integration.Http;

namespace Fusion.Resources.Functions.Common.ApiClients;

public class RolesApiClient : IRolesApiClient
{
    private readonly HttpClient rolesClient;

    public RolesApiClient(IHttpClientFactory clientFactory)
    {
        rolesClient = clientFactory.CreateClient(HttpClientNames.Application.Roles);
    }

    private static string GetActiveOrgAdminsOdataQuery() => "scope.type eq 'OrgChart' and roleName eq 'Fusion.OrgChart.Admin' and source eq 'Fusion.Roles' and " +
                                                            $"validTo gteq '{DateTime.UtcNow:O}'";

    public async Task<Dictionary<Guid, ICollection<ApiSinglePersonRole>>> GetAdminRolesForOrgProjects(IEnumerable<Guid> projectIds)
    {
        var odataQuery = new ODataQuery();

        odataQuery.Filter = GetActiveOrgAdminsOdataQuery() + $" and scope.value in ({string.Join(',', projectIds.Select(p => $"'{p}'"))})";

        var url = ODataQuery.ApplyQueryString("/roles", odataQuery);
        var data = await rolesClient.GetAsJsonAsync<List<ApiSinglePersonRole>>(url);

        return data.GroupBy(r => Guid.Parse(r.Scope.Value))
            .ToDictionary(g => g.Key, ICollection<ApiSinglePersonRole> (g) => g.ToArray());
    }

    public async Task<Dictionary<Guid, ICollection<ApiSinglePersonRole>>> GetExpiringAdminRolesForOrgProjects(IEnumerable<Guid> projectIds, int monthsUntilExpiry)
    {
        var odataQuery = new ODataQuery();

        var expiryDate = DateTime.UtcNow.AddMonths(monthsUntilExpiry).ToString("O");

        odataQuery.Filter = GetActiveOrgAdminsOdataQuery() + $" and validTo lteq '{expiryDate}' and scope.value in ({string.Join(',', projectIds.Select(p => $"'{p}'"))})";

        var url = ODataQuery.ApplyQueryString("/roles", odataQuery);
        var data = await rolesClient.GetAsJsonAsync<List<ApiSinglePersonRole>>(url);

        return data.GroupBy(r => Guid.Parse(r.Scope.Value))
            .ToDictionary(g => g.Key, ICollection<ApiSinglePersonRole> (g) => g.ToArray());
    }
}