﻿using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Authorization;
using Fusion.Resources.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Controllers.Projects
{
    [Authorize]
    [ApiController]
    public class ProjectsController : ResourceControllerBase
    {
        [HttpGet("projects")]
        public async Task<ActionResult<ApiProjectReference>> GetProjects()
        {
            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlExternal();
                r.AnyOf(a => a.BeTrustedApplication());
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            var projectList = await DispatchAsync(new GetProjects());
            var apiResult = projectList.Select(p => new ApiProjectReference(p));

            return Ok(apiResult);
        }
    }
}
