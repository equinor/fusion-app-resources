using System.Collections.Generic;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiSectorDepartments
    {
        public string SectorString { get; set; }
        //responsible
        public List<DepartmentInSector> DepartmentsInSector { get; set; }
    }

    public class DepartmentInSector
    {
        public List<ApiInternalPersonnelPerson> DepartmentPersonnel { get; set; }
        public string DepartmentString { get; set; }

    }
}