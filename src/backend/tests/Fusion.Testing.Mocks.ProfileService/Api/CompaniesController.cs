using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using Fusion.AspNetCore.OData;

namespace Fusion.Testing.Mocks.ProfileService.Api
{
    [ApiController]
    [ApiVersion("1.0")]
    public class CompaniesController : ControllerBase
    {
        [HttpGet("/companies")]
        public List<ApiCompanyInfo> ListCompanies([FromQuery] ODataQueryParams @params)
        {
            var query = PeopleServiceMock.companies.AsQueryable();

            // Support search
            if (@params != null && !string.IsNullOrEmpty(@params.Search))
            {
                query = query.Where(c => c.Name.Contains(@params.Search));
            }

            var companies = query.OrderBy(c => c.Name).Select(c => new ApiCompanyInfo
            {
                Id = c.Id,
                Name = c.Name
            }).ToList();

            return companies;
        }
    }
}
