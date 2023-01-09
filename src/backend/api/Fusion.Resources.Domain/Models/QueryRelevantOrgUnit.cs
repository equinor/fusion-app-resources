using System.Collections.Generic;

namespace Fusion.Resources.Domain.Models
{
    public class QueryRelevantOrgUnit
    {
        public string SapId { get; set; } = "";
        public string FullDepartment { get; set; } = "";
        public List<string> Reasons { get; set; } = new();
        public string Name { get; set; } = "";
        public string ParentSapId { get; set; } = "";
        public string ShortName { get; set; } = "";
        public string Department { get; set; } = "";


    }
}