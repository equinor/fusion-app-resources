using Fusion.Resources.Domain;

namespace Fusion.Resources.Api.Controllers.Departments
{
    public class ApiDepartment
    {
        public ApiDepartment(QueryDepartmentWithResponsible department)
        {
            Name = department.Name;
            Sector = department.Sector;
            Responsible = (department.Responsible is not null) ? new ApiPerson(department.Responsible) : null;
        }

        public string Name { get; set; }
        public string? Sector { get; set; }
        public ApiPerson? Responsible { get; set; }
    }
}
