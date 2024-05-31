using Azure;
using Azure.Core;
using Fusion.Resources.Application.LineOrg;
using Fusion.Services.LineOrg.ApiModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Resources
{
    public interface ILineOrgClient
    {
        /// <summary>
        /// Resolve a specific org unit. 
        /// 
        /// Will return null if org unit is not found (404), any other non successfull status codes will throw exception.
        /// 
        /// </summary>
        /// <param name="identifier">Full department string or SAP id. Prefer sap id</param>
        /// <param name="expand">Optionally expand properties</param>
        /// <returns>OrgUnit or null</returns>
        Task<ApiOrgUnit?> ResolveOrgUnitAsync(string identifier, Action<LineOrgClient.OrgUnitExpand>? expand);
        
        /// <summary>
        /// Load all org units from the line org service, with management expanded.
        /// 
        /// This will handle caching and should be safe to call repeatedly.
        /// 
        /// Cache will be invalided on change event from line org api.
        /// </summary>
        Task<List<ApiOrgUnit>> LoadAllOrgUnitsAsync();
    }

}
