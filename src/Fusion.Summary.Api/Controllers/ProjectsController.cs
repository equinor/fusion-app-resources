using System.Net.Mime;
using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Authorization;
using Fusion.Integration.Profile;
using Fusion.Summary.Api.Authorization.Extensions;
using Fusion.Summary.Api.Controllers.ApiModels;
using Fusion.Summary.Api.Controllers.Requests;
using Fusion.Summary.Api.Domain.Commands;
using Fusion.Summary.Api.Domain.Queries;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fusion.Summary.Api.Controllers;

[Authorize]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
[ApiVersion("1.0")]
public class ProjectsController : BaseController
{
    [HttpGet("projects")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiProject[]>> GetProjectsV1()
    {
        #region Authorization

        var authResult = await Request.RequireAuthorizationAsync(r =>
        {
            r.AlwaysAccessWhen().ResourcesFullControl();
            r.AnyOf(or => { or.BeTrustedApplication(); });
        });

        if (authResult.Unauthorized)
            return authResult.CreateForbiddenResponse();

        #endregion Authorization

        var projects = await DispatchAsync(new GetProjects());

        var apiProjects = projects.Select(ApiProject.FromQueryProject);

        return Ok(apiProjects);
    }


    [HttpGet("projects/{projectId:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiProject?>> GetProjectsV1(Guid projectId)
    {
        #region Authorization

        var authResult = await Request.RequireAuthorizationAsync(r =>
        {
            r.AlwaysAccessWhen().ResourcesFullControl();
            r.AnyOf(or => { or.BeTrustedApplication(); });
        });

        if (authResult.Unauthorized)
            return authResult.CreateForbiddenResponse();

        #endregion Authorization

        var projects = await DispatchAsync(new GetProjects().WhereProjectId(projectId));

        var apiProjects = projects.Select(ApiProject.FromQueryProject);

        return Ok(apiProjects.FirstOrDefault());
    }

    [HttpPut("projects/{projectId:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiProject?>> PutProjectsV1(Guid projectId, PutProjectRequest request)
    {
        #region Authorization

        var authResult = await Request.RequireAuthorizationAsync(r =>
        {
            r.AlwaysAccessWhen().ResourcesFullControl();
            r.AnyOf(or => { or.BeTrustedApplication(); });
        });

        if (authResult.Unauthorized)
            return authResult.CreateForbiddenResponse();

        #endregion Authorization

        if (projectId == Guid.Empty)
            return FusionApiError.InvalidOperation("ProjectIdRequired", "ProjectId route parameter is required and cannot be empty");

        var project = (await DispatchAsync(new GetProjects().WhereProjectId(projectId))).FirstOrDefault();

        if (project == null)
        {
            project = await DispatchAsync(new CreateProject(request).WithProjectId(projectId));

            return Created(Request.GetUri(), ApiProject.FromQueryProject(project));
        }

        project = await DispatchAsync(new UpdateProject(project.Id, request));

        return Ok(ApiProject.FromQueryProject(project));
    }
}
