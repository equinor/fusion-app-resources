using System;
using System.Collections.Generic;

namespace Fusion.Resources.Api.Controllers.Personnel.ApiModels
{
    public class ApiSector
    {
        public ApiSector(string fullDepartmentString)
        {
            this.FullDepartmentString = fullDepartmentString;
        }
        public string FullDepartmentString { get; set; }
        public List<SectorDepartment> SectorDepartments { get; set; } = new List<SectorDepartment>();
        
        public class SectorDepartment
        {
            public SectorDepartment(string fullDepartmentString, List<ApiInternalPersonnelPerson> departmentPersonnel)
            {
                this.FullDepartmentString = fullDepartmentString;
                this.DepartmentPersonnel = departmentPersonnel;
            }
            public string FullDepartmentString { get; set; }
            public List<ApiInternalPersonnelPerson> DepartmentPersonnel { get; set; } = new List<ApiInternalPersonnelPerson>();
        }

    }
}
