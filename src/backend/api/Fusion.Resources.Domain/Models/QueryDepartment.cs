using Fusion.Integration.Profile;
using Fusion.Resources.Database.Entities;

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

        public FusionPersonProfile? LineOrgResponsible { get; }
        public FusionPersonProfile? DefactoResponsible { get; set; }
    }
}
