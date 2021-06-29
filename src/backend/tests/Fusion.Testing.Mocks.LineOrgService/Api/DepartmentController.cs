using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using Fusion.AspNetCore.OData;

namespace Fusion.Testing.Mocks.LineOrgService.Api
{
    [ApiController]
    [ApiVersion("1.0")]
    public class DepartmentController : ControllerBase
    {
        [HttpGet("lineorg/departments/{department}")]
        public List<ApiDepartment> GetSingleDepartment(string department, [FromQuery] ODataQueryParams @params)
        {
            var query = LineOrgServiceMock.Departments.AsQueryable();

            // Support search
            if (@params != null && !string.IsNullOrEmpty(@params.Search))
            {
                query = query.Where(c => c.Name.Contains(@params.Search));
            }

            var companies = query.OrderBy(c => c.Name).Select(c => new ApiDepartment
            {
                Name = c.Name
            }).ToList();

            return companies;
        }
    }
}
