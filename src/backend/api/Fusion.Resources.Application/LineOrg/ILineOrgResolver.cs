using Fusion.Resources.Application.LineOrg.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Application.LineOrg
{
    public interface ILineOrgResolver
    {
        Task<List<LineOrgDepartment>> GetResourceOwners(string? filter, CancellationToken cancellationToken);
    }
}
