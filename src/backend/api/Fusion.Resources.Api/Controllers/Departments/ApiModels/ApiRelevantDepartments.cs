using System.Collections.Generic;

namespace Fusion.Resources.Api.Controllers.Departments
{
    public class ApiRelevantDepartments
    {
        public ApiDepartment? Department { get; set; }
        public List<ApiDepartment> Relevant { get; set; } = new();
    }
}
