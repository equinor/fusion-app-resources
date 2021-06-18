using Fusion.Resources.Api.Controllers.Departments;
using Fusion.Resources.Domain;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiRelevantDepartments
    {
        public ApiRelevantDepartments(QueryRelevantDepartments relevantDepartments)
        {
            Children = relevantDepartments.Children is not null 
                ? relevantDepartments.Children.Select(x => new ApiDepartment(x)).ToList()
                : new List<ApiDepartment>();
            Siblings = relevantDepartments.Siblings is not null
                ? relevantDepartments.Siblings.Select(x => new ApiDepartment(x)).ToList()
                : new List<ApiDepartment>();
        }
        public List<ApiDepartment> Children { get; set; }
        public List<ApiDepartment> Siblings { get; set; }
    }
}
