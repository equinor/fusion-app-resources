using System;
using System.Net.Http;
using System.Threading.Tasks;
using Fusion.Resources.Functions.Integration;

namespace Fusion.Resources.Functions.ApiClients;

public class PeopleApiClient : IPeopleApiClient
{
    private readonly HttpClient peopleClient;

    public PeopleApiClient(IHttpClientFactory httpClientFactory)
    {
        peopleClient = httpClientFactory.CreateClient(HttpClientNames.Application.People);
        peopleClient.Timeout = TimeSpan.FromMinutes(5);
    }

    public async Task<string> GetPersonFullDepartmentAsync(Guid? personAzureUniqueId)
    {
        var data = await peopleClient.GetAsJsonAsync<IResourcesApiClient.Person>(
            $"persons/{personAzureUniqueId}?api-version=3.0");

        return data.FullDepartment;
    }
}