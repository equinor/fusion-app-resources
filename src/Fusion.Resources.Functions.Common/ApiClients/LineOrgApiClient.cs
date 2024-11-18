#nullable enable
using Fusion.Resources.Functions.Common.ApiClients.ApiModels;
using Fusion.Resources.Functions.Common.Integration.Http;

namespace Fusion.Resources.Functions.Common.ApiClients;

public class LineOrgApiClient : ILineOrgApiClient
{
    private readonly HttpClient lineOrgClient;

    public LineOrgApiClient(IHttpClientFactory httpClientFactory)
    {
        lineOrgClient = httpClientFactory.CreateClient(HttpClientNames.Application.LineOrg);
        lineOrgClient.Timeout = TimeSpan.FromMinutes(5);
    }

    public async Task<IEnumerable<OrgUnits>> GetOrgUnitDepartmentsAsync()
    {
        var data =
            await lineOrgClient.GetAsJsonAsync<InternalCollection<OrgUnits>>($"/org-units?$top={int.MaxValue}&$expand=management");

        return data.Value
            .Where(x => !string.IsNullOrEmpty(x.FullDepartment))
            .ToList();
    }

    public async Task<List<LineOrgPerson>> GetResourceOwnersFromFullDepartment(ICollection<OrgUnits> fullDepartments)
    {
        var list = fullDepartments
            .Select(l => $"'{l.FullDepartment?.Replace("&", "%26")}'")
            .ToList()
            .Aggregate((a, b) => $"{a}, {b}");
        var queryString = $"/lineorg/persons?$filter=fullDepartment in " +
                          $"({list}) " +
                          $"and isResourceOwner eq 'true'";
        var resourceOwners = await lineOrgClient.GetAsJsonAsync<LineOrgPersonsResponse>(queryString);
        foreach (var r in resourceOwners.Value)
            r.DepartmentSapId = fullDepartments.FirstOrDefault(x => x.FullDepartment == r.FullDepartment)?.SapId;

        return resourceOwners.Value;
    }

    public class OrgUnits
    {
        public string? FullDepartment { get; set; }
        public string? SapId { get; set; }
        public Management Management { get; set; }
        public int Level { get; set; }
    }

    public class Management
    {
        public Person[] Persons { get; set; }
    }

    public class Person
    {
        public string AzureUniqueId { get; set; }
    }

    internal class InternalCollection<T>
    {
        public InternalCollection(IEnumerable<T> items)
        {
            Value = items;
        }

        public IEnumerable<T> Value { get; set; }
    }

}