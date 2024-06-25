using Fusion.Resources.Functions.Common.ApiClients.ApiModels;

namespace Fusion.Resources.Functions.Common.ApiClients;

public interface ILineOrgApiClient
{
    Task<IEnumerable<LineOrgApiClient.OrgUnits>> GetOrgUnitDepartmentsAsync();
    Task<List<LineOrgPerson>> GetResourceOwnersFromFullDepartment(List<LineOrgApiClient.OrgUnits> fullDepartments);
}