using Fusion.Integration.Profile;
using Fusion.Resources.Database.Entities;

namespace Fusion.Resources.Domain
{
    public class QueryDepartmentWithResponsible
    {
        public QueryDepartmentWithResponsible(DbDepartment department, FusionPersonProfile? responsible)
        {
            Name = department.DepartmentId;
            Sector = department.SectorId;

            LineOrgResponsible = responsible;
        }

        public string Name { get; }
        public string? Sector { get; }
        public FusionPersonProfile? LineOrgResponsible { get; }
        public FusionPersonProfile? DefactoResponsible { get; set; }
    }
}
