using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion.Resources.Functions.ApiClients.ApiModels;

namespace Fusion.Resources.Functions.ApiClients;

public interface ILineOrgApiClient
{
    Task<IEnumerable<LineOrgApiClient.OrgUnits>> GetOrgUnitDepartmentsAsync();
    Task<List<LineOrgPerson>> GetResourceOwnersFromFullDepartment(List<LineOrgApiClient.OrgUnits> fullDepartments);
}