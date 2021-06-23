using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using Fusion.AspNetCore.OData;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Fusion.Testing.Mocks.LineOrgService.Api
{
    [ApiController]
    [ApiVersion("1.0")]
    public class LineOrgPersonsController : ControllerBase
    {
        [HttpGet("lineorg/persons")]
        public async Task<ActionResult<ApiPagedCollection<ApiLineOrgUser>>> GetUsers([FromQuery] ODataQueryParams queryParams)
        {
            var result = new ApiPagedCollection<ApiLineOrgUser>(LineOrgServiceMock.Users.ToList(), LineOrgServiceMock.Users.Count);
            return result;
        }
        [HttpGet("lineorg/persons/{azureUniqueId}")]
        public async Task<ActionResult<ApiLineOrgUser>> GetSingleUser(Guid azureUniqueId, [FromQuery] ODataQueryParams queryParams)
        {

            var user = LineOrgServiceMock.Users.FirstOrDefault(p => p.AzureUniqueId == azureUniqueId);

            if (user == null)
                return NotFound();

            // Must take a copy, as we don't want to change the "database"
            var copy = JsonConvert.DeserializeObject<ApiLineOrgUser>(JsonConvert.SerializeObject(user));


            return copy;
        }

    }
}
