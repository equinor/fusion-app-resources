using Microsoft.AspNetCore.Mvc;
using Fusion.Services.Org.ApiModels;

namespace Fusion.Testing.Mocks.OrgService.Api.Controllers
{

    [ApiController]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    public class ProjectsController : ControllerBase
    {
        [HttpGet("/projects/{projectIdentifier}")]
        public ActionResult<ApiProjectV2> GetProject([FromRoute] ProjectIdentifier projectIdentifier)
        {
            var project = projectIdentifier.GetProjectOrDefault();

            if (project != null)
                return project;

            return NotFound();
        }
    }
}