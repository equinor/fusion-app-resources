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
            DelegatedResponsibles = (department.DelegatedResourceOwners is not null) 
                ? department.DelegatedResourceOwners.Select(x => new ApiPerson(x)).ToList() 
                : null;
        }

        public ApiDepartment(QueryDepartment department, QueryDepartmentAutoApprovalStatus? approvalStatus) : this(department)
        {
            if (approvalStatus is not null)
                AutoApproval = new ApiDepartmentAutoApproval(approvalStatus);
        }

        public string Name { get; set; }
        public string? Sector { get; set; }
        public ApiPerson? LineOrgResponsible { get; set; }
        public List<ApiPerson>? DelegatedResponsibles { get; set; }

        public ApiDepartmentAutoApproval? AutoApproval { get; set; }
    }

    public class ApiDepartmentAutoApproval
    {
        public ApiDepartmentAutoApproval(QueryDepartmentAutoApprovalStatus approvalStatus)
        {
            Enabled = approvalStatus.Enabled;
            Mode = approvalStatus.IncludeSubDepartments ? ApiDepartmentAutoApprovalMode.All : ApiDepartmentAutoApprovalMode.Direct;
            Inherited = approvalStatus.Inherited;
            InheritedFrom = approvalStatus.EffectiveDepartmentPath;
        }

        public bool Enabled { get; set; }
        public ApiDepartmentAutoApprovalMode Mode { get; set; }
        public bool Inherited { get; set; }
        public string? InheritedFrom { get; set; }
    }
}
