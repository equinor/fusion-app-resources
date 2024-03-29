using Fusion.Integration.Profile;
using Fusion.Services.LineOrg.ApiModels;
using System.Collections.Generic;

namespace Fusion.Resources.Domain
{
    public class QueryDepartment
    {
        public QueryDepartment(ApiDepartment lineOrgDepartment, FusionPersonProfile? manager)
        {
            DepartmentId = lineOrgDepartment.FullName;
            LineOrgResponsible = manager;
        }

        public QueryDepartment(string departmentId, string? sectorId)
        {
            DepartmentId = departmentId;
            SectorId = sectorId;
        }

        public string DepartmentId { get; }
        public string? SectorId { get; }

        public FusionPersonProfile? LineOrgResponsible { get; set; }
        public List<FusionPersonProfile>? DelegatedResourceOwners { get; set; }
        public bool IsTracked { get; set; } = false;
    }
}
