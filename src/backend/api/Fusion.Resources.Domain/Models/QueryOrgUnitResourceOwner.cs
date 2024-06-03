using Fusion.Services.LineOrg.ApiModels;
using System.Collections.Generic;

namespace Fusion.Resources.Domain
{
    public class QueryOrgUnitResourceOwner
    {
        public QueryOrgUnitResourceOwner(string sapid, string fullDepartment)
        {
            OrgUnitIdentifier = sapid;
            FullDepartment = fullDepartment;
        }

        public string OrgUnitIdentifier { get; }
        public string FullDepartment { get; set; }

        public ApiPerson? MainManager { get; set; }

        public List<ApiPerson> AllManagers { get; set; } = new List<ApiPerson>();

    }

}