using Fusion.Resources.Domain;
using System.Collections.Generic;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiRelevantDepartments
    {
        public ApiRelevantDepartments(QueryRelevantDepartments relevantDepartments)
        {
            Children = relevantDepartments.Children;
            Siblings = relevantDepartments.Siblings;
        }
        public List<string> Children { get; set; }
        public List<string> Siblings { get; set; }

    }
}
