using System.Collections.Generic;
using System.Linq;

namespace Fusion.Resources.Domain.Models
{
    public class QueryRelevantDepartmentProfile
    {
        public QueryRelevantDepartmentProfile(string? fullDepartment, List<string> reason, string? sapId, string? parentSapId, string? shortName, string? deparment, string? name)
        {
            this.fullDepartment = fullDepartment;
            this.name = name;
            this.reason = reason;
            this.sapId = sapId;
            this.parentSapId = parentSapId;
            this.shortName = shortName;
            this.department = deparment;



        }

        public string? name { get; set; }
        public string? sapId { get; set; }
        public string? parentSapId { get; set; }
        public string? shortName { get; set; }
        public string? department { get; set; }
        public string? fullDepartment { get; set; }

        public List<string?> reason { get; set; }

    }
}
