using Fusion.Resources.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Controllers
{
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    public class AdminController : ResourceControllerBase
    {

        private readonly IOrgUnitCache orgUnitCache;

        public AdminController(IOrgUnitCache orgUnitCache)
        {

            this.orgUnitCache = orgUnitCache;
        }

        [HttpGet("admin/cache/org-units")]

        public async Task<ActionResult> CleareCache()
        {
            await orgUnitCache.ClearOrgUnitCacheAsync();

            return Ok();
        }

      
    }
}