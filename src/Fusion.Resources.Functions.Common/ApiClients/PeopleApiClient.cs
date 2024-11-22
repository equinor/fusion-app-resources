using Fusion.Integration.Profile;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Resources.Functions.Common.Integration.Http;

namespace Fusion.Resources.Functions.Common.ApiClients;

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

    public async Task<ICollection<ApiEnsuredProfileV2>> ResolvePersonsAsync(IEnumerable<PersonIdentifier> personAzureUniqueIds, CancellationToken cancellationToken = default)
    {
        var resp = await peopleClient
            .PostAsJsonAsync<ICollection<ApiEnsuredProfileV2>>($"/persons/ensure?api-version=3.0", new
            {
                personIdentifiers = personAzureUniqueIds.Select(p => p.ToString())
            }, cancellationToken);

        return resp;
    }
}