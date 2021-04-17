using System.Collections.Generic;
using System.Linq;

namespace Fusion.Resources.Domain.Queries
{
    public class QueryResourceOwnerProfile
    {
        public QueryResourceOwnerProfile(string? fullDepartment, bool isDepartmentManager, IEnumerable<string> departmentsWithResponsibility, IEnumerable<string> relevantSectors)
        {
            FullDepartment = fullDepartment;
            IsDepartmentManager = isDepartmentManager;
            DepartmentsWithResponsibility = departmentsWithResponsibility.ToList();
            RelevantSectors = relevantSectors.ToList();
        }

        public string? FullDepartment { get; set; }
        public bool IsDepartmentManager { get; set; }
        public List<string> DepartmentsWithResponsibility { get; set; }
        public List<string> RelevantSectors { get; set; }

        /// <summary>
        /// Property resolved from line org, could be null if the integration fails.
        /// </summary>
        public List<string>? ChildDepartments { get; set; }
        /// <summary>
        /// Property resolved from line org, could be null if the integration fails.
        /// </summary>
        public List<string>? SiblingDepartments { get; set; }
        public string? Sector { get; set; }
    }
}
