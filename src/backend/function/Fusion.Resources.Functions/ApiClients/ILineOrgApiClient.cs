using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.ApiClients;

public interface ILineOrgApiClient
{
    Task<IEnumerable<string>> GetOrgUnitDepartmentsAsync();
}