using Fusion.Integration.Profile;
using Fusion.Resources.Database.Entities;
using System.Collections.Generic;

namespace Fusion.Resources.Domain
{
    public class QueryDepartment
    {
        public QueryDepartment(DbDepartment department)
        {
            DepartmentId = department.DepartmentId;
            SectorId = department.SectorId;
        }

        public QueryDepartment(string departmentId, string? sectorId)
        {
            DepartmentId = departmentId;
            SectorId = sectorId;
        }

        public QueryDepartment(DbDepartment department, FusionPersonProfile? responsible)
        {
            DepartmentId = department.DepartmentId;
            SectorId = department.SectorId;

            LineOrgResponsible = responsible;
        }

        public string DepartmentId { get; }
        public string? SectorId { get; }

        public FusionPersonProfile? LineOrgResponsible { get; set; }
        public List<FusionPersonProfile>? DelegatedResourceOwners { get; set; }
    }
}
