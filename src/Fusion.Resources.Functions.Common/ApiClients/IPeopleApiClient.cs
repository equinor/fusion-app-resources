namespace Fusion.Resources.Functions.Common.ApiClients;

public interface IPeopleApiClient
{
    Task<string> GetPersonFullDepartmentAsync(Guid? personAzureUniqueId);
}