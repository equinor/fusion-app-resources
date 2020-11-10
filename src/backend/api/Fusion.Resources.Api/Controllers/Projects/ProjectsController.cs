using Fusion.AspNetCore.FluentAuthorization;
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
        public async Task<ActionResult<ApiProject>> GetProjects()
        {
            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();
                r.AnyOf(a => a.BeTrustedApplication());
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            var projectList = await DispatchAsync(new GetProjects());
            var apiResult = projectList.Select(p => new ApiProject(p));

            return Ok(apiResult);
        }
    }
}
