using System.Net.Http.Json;
using System.Text.Json;
using Fusion.Integration.Http.Models;
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
                                                            $"validTo gt '{DateTime.UtcNow:O}'";

    public async Task<Dictionary<Guid, ICollection<ApiSinglePersonRole>>> GetAdminRolesForOrgProjects(ICollection<Guid> projectIds, CancellationToken cancellationToken = default)
    {
        var odataQuery = new ODataQuery();

        odataQuery.Filter = GetActiveOrgAdminsOdataQuery();

        var url = ODataQuery.ApplyQueryString("/roles", odataQuery);
        var data = await rolesClient.GetFromJsonAsync<List<ApiSinglePersonRole>>(url, cancellationToken: cancellationToken)
                   ?? throw new InvalidOperationException("Roles response was null");

        return data
            // Filter roles to projects in memory to avoid a very lage OData query (url)
            .Where(r => Guid.TryParse(r.Scope.Value, out var projectId) && projectIds.Contains(projectId))
            .GroupBy(r => Guid.Parse(r.Scope.Value))
            .ToDictionary(g => g.Key, ICollection<ApiSinglePersonRole> (g) => g.ToArray());
    }
}