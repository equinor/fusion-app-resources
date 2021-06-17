using System.Collections.Generic;

namespace Fusion.Resources.Domain
{
    public class QueryRelevantDepartments
    {
        public List<string> Children { get; set; } = new List<string>();
        public List<string> Siblings { get; set; } = new List<string>();
    }
}
