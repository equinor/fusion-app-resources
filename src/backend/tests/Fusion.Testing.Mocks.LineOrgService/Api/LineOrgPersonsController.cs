using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using Fusion.AspNetCore.OData;

namespace Fusion.Testing.Mocks.LineOrgService.Api
{
    [ApiController]
    [ApiVersion("1.0")]
    public class LineOrgPersonsController : ControllerBase
    {
        [HttpGet("lineorg/persons")]
        public ActionResult<ApiPagedCollection<ApiLineOrgUser>> GetUsers([FromQuery] ODataQueryParams queryParams)
        {
            var result = new ApiPagedCollection<ApiLineOrgUser>(LineOrgServiceMock.Users.ToList(), LineOrgServiceMock.Users.Count);
            return result;
        }
        [HttpGet("lineorg/persons/{azureUniqueId}")]
        public ActionResult<ApiLineOrgUser> GetSingleUser(Guid azureUniqueId, [FromQuery] ODataQueryParams queryParams)
        {
            var user = LineOrgServiceMock.Users.FirstOrDefault(p => p.AzureUniqueId == azureUniqueId);

            if (user == null)
                return NotFound();

            return user;
        }

    }
}
