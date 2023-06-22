using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Authorization;
using Fusion.Integration.LineOrg;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands.Departments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Controllers
{
    [ApiVersion("1.0-preview")]
    [ApiVersion("1.0")]
    [Authorize]
    [ApiController]
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