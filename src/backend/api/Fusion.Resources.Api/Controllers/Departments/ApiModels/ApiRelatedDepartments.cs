using Fusion.Resources.Domain;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiRelatedDepartments
    {
        public ApiRelatedDepartments(QueryRelatedDepartments relevantDepartments)
        {
            Parent = new ApiDepartment(relevantDepartments.Parent);
            Children = relevantDepartments.Children.Select(x => new ApiDepartment(x)).ToList();
            Siblings = relevantDepartments.Siblings.Select(x => new ApiDepartment(x)).ToList();
        }

        public ApiDepartment Parent { get; }
        public List<ApiDepartment> Children { get; }
        public List<ApiDepartment> Siblings { get; }
    }
}
