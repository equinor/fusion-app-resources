using Fusion.Resources.Database.Entities;

namespace Fusion.Resources.Domain.Models
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

        public string DepartmentId { get; }
        public string? SectorId { get; }
    }
}
