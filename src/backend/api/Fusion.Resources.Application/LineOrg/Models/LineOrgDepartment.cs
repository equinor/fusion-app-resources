using Fusion.Integration.Profile;

namespace Fusion.Resources.Application.LineOrg.Models
{
    public class LineOrgDepartment
    {
        public LineOrgDepartment(string fullDepartment)
        {
            DepartmentId = fullDepartment;
        }

        public string DepartmentId { get; set; }
        public FusionPersonProfile Responsible { get; internal set; }
    }
}
