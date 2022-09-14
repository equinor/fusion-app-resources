using System;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.ApiClients;

public interface IPeopleApiClient
{
    Task<string> GetPersonFullDepartmentAsync(Guid? personAzureUniqueId);
}