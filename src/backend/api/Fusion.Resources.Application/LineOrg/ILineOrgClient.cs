using Azure;
using Azure.Core;
using Fusion.Resources.Application.LineOrg;
using Fusion.Services.LineOrg.ApiModels;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Resources
{
    public interface ILineOrgClient
    {
        Task<ApiOrgUnit?> ResolveOrgUnitAsync(string identifier, Action<LineOrgClient.OrgUnitExpand>? expand);
    }

}
