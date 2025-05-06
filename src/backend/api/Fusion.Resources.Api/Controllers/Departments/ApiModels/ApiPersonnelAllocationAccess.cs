using Fusion.Resources.Domain;

namespace Fusion.Resources.Api.Controllers;

public class ApiPersonnelAllocationAccess
{
    public ApiPersonnelAllocationAccess(QueryDepartment department)
    {
        Department = new ApiDepartment(department);
    }

    public ApiDepartment Department { get; set; }
}