using Fusion.Resources.Domain;

namespace Fusion.Resources.Api.Controllers.Departments
{
    public class ApiDepartment
    {
        public ApiDepartment(QueryDepartmentWithResponsible department)
        {
            Name = department.Name;
            Sector = department.Sector;
            LineOrgResponsible = (department.LineOrgResponsible is not null) ? new ApiPerson(department.LineOrgResponsible) : null;
            DefactoResponsible = (department.DefactoResponsible is not null) ? new ApiPerson(department.DefactoResponsible) : null;
        }

        public string Name { get; set; }
        public string? Sector { get; set; }
        public ApiPerson? LineOrgResponsible { get; set; }
        public ApiPerson? DefactoResponsible { get; set; }

    }
}
