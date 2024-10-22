using System.Net.Mime;
using Fusion.AspNetCore.FluentAuthorization;
using Fusion.AspNetCore.OData;
using Fusion.Authorization;
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
public class TaskOwnerSummaryController : BaseController
{
    [HttpGet("task-owners-summary-reports/{projectId:guid}/weekly")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ODataTop(100), ODataSkip]
    public async Task<ActionResult<ApiCollection<ApiWeeklyTaskOwnerReport>>> GetWeeklyTaskOwnerReportsV1(Guid projectId, ODataQueryParams query)
    {
        #region Authorization

        var authResult =
            await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().ResourcesFullControl();
                r.AnyOf(or => { or.BeTrustedApplication(); });
            });

        if (authResult.Unauthorized)
            return authResult.CreateForbiddenResponse();

        #endregion

        if ((await DispatchAsync(new GetProjects().WhereProjectId(projectId))).FirstOrDefault() is null)
            return ProjectNotFound(projectId);


        var projects = await DispatchAsync(new GetWeeklyTaskOwnerReports(projectId, query));

        return Ok(ApiCollection<ApiWeeklyTaskOwnerReport>.FromQueryCollection(projects, ApiWeeklyTaskOwnerReport.FromQueryWeeklyTaskOwnerReport));
    }

    [HttpGet("task-owners-summary-reports/{projectId:guid}/weekly/{reportId:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiWeeklyTaskOwnerReport?>> GetWeeklyTaskOwnerReportV1(Guid projectId, Guid reportId)
    {
        #region Authorization

        var authResult =
            await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().ResourcesFullControl();
                r.AnyOf(or => { or.BeTrustedApplication(); });
            });

        if (authResult.Unauthorized)
            return authResult.CreateForbiddenResponse();

        #endregion

        if ((await DispatchAsync(new GetProjects().WhereProjectId(projectId))).FirstOrDefault() is null)
            return ProjectNotFound(projectId);

        var report = (await DispatchAsync(new GetWeeklyTaskOwnerReports(projectId, new ODataQueryParams()).WhereReportId(reportId))).FirstOrDefault();

        return report is null ? NotFound() : Ok(ApiWeeklyTaskOwnerReport.FromQueryWeeklyTaskOwnerReport(report));
    }

    /// <summary>
    ///     Summary report key is composed of the project id and the period start and end dates.
    ///     If a report already exists for the given period and project id, it will be replaced.
    /// </summary>
    [HttpPut("task-owners-summary-reports/{projectId:guid}/weekly")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PutWeeklyTaskOwnerReportV1(Guid projectId, [FromBody] PutWeeklyTaskOwnerReportRequest request)
    {
        #region Authorization

        var authResult =
            await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().ResourcesFullControl();
                r.AnyOf(or => { or.BeTrustedApplication(); });
            });

        if (authResult.Unauthorized)
            return authResult.CreateForbiddenResponse();

        #endregion

        var project = (await DispatchAsync(new GetProjects().WhereProjectId(projectId))).FirstOrDefault();
        if (project is null)
            return ProjectNotFound(projectId);

        var command = new PutWeeklyTaskOwnerReport(project.Id, request);

        var newReportCreated = await DispatchAsync(command);

        return newReportCreated ? Created(Request.GetUri(), null) : NoContent();
    }
}