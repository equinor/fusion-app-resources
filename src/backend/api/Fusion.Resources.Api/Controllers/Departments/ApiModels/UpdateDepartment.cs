using System;

namespace Fusion.Resources.Api.Controllers.Departments
{
    public class UpdateDepartment
    {
        public string SectorOrgPath { get; set; }
        public Guid Responsible { get; set; }
        public OrgTypes OrgType { get; set; }
    }
}
