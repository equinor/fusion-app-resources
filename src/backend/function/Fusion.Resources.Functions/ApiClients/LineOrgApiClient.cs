#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fusion.Resources.Functions.Integration;

namespace Fusion.Resources.Functions.ApiClients;

public class LineOrgApiClient : ILineOrgApiClient
{
    private readonly HttpClient lineOrgClient;

    public LineOrgApiClient(IHttpClientFactory httpClientFactory)
    {
        lineOrgClient = httpClientFactory.CreateClient(HttpClientNames.Application.LineOrg);
    }

    public async Task<IEnumerable<string>> GetOrgUnitDepartmentsAsync()
    {
        var data =
            await lineOrgClient.GetAsJsonAsync<InternalCollection<DepartmentRef>>($"/org-units?$top={int.MaxValue}");

        return data.Value
            .Where(x => !string.IsNullOrEmpty(x.FullDepartment))
            .Select(x => x.FullDepartment!).ToList();
    }

    internal class DepartmentRef
    {
        public string? FullDepartment { get; set; }
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