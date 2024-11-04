using Fusion.Integration.Profile;
using Fusion.Integration.Profile.ApiClient;

namespace Fusion.Resources.Functions.Common.ApiClients;

public interface IPeopleApiClient
{
    Task<string> GetPersonFullDepartmentAsync(Guid? personAzureUniqueId);

    Task<ICollection<ApiEnsuredProfileV2>> ResolvePersonsAsync(IEnumerable<PersonIdentifier> personAzureUniqueIds, CancellationToken cancellationToken = default); 
}