using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion.Resources.Functions.ApiClients.ApiModels;

namespace Fusion.Resources.Functions.ApiClients;

public interface ILineOrgApiClient
{
    Task<IEnumerable<string>> GetOrgUnitDepartmentsAsync();
    Task<List<LineOrgPerson>> GetResourceOwnersFromFullDepartment(List<string> fullDepartments);
}