using System.Collections.Generic;

namespace Fusion.Resources.Domain.Models
{
    public class QueryRelevantOrgUnit
    {
        public string SapId { get; set; } = null!;
        public string FullDepartment { get; set; } = null!;
        public List<string> Reasons { get; set; } = new();
        public string Name { get; set; } = null!;
        public string ParentSapId { get; set; } = null!;
        public string ShortName { get; set; } = null!;
        public string Department { get; set; } = null!;
    }
}