using System.Collections.Generic;

namespace Fusion.Resources.Domain
{
    public class QueryRelatedDepartments
    {
        public List<QueryDepartment> Children { get; set; } = new List<QueryDepartment>();
        public List<QueryDepartment> Siblings { get; set; } = new List<QueryDepartment>();
    }
}
