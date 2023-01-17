using Fusion.Resources.Domain;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiDepartment
    {
        public ApiDepartment(QueryDepartment department)
        {
            Name = department.DepartmentId;
            Sector = department.SectorId;
            LineOrgResponsible = (department.LineOrgResponsible is not null) ? new ApiPerson(department.LineOrgResponsible) : null;
            DelegatedResponsibles = department.DelegatedResourceOwners?.Select(x => new ApiPersonDelegatedResponsibility(x)).ToList();
        }

        public string Name { get; set; }
        public string? Sector { get; set; }
        public ApiPerson? LineOrgResponsible { get; set; }
        public List<ApiPersonDelegatedResponsibility>? DelegatedResponsibles { get; set; }

    }
}
