using System.Net.Mime;
using Fusion.AspNetCore.FluentAuthorization;
using Fusion.AspNetCore.OData;
using Fusion.Authorization;
using Fusion.Summary.Api.Authorization.Extensions;
using Fusion.Summary.Api.Controllers.ApiModels;
using Fusion.Summary.Api.Controllers.Requests;
using Fusion.Summary.Api.Domain.Commands;
using Fusion.Summary.Api.Domain.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fusion.Summary.Api.Controllers;

[Authorize]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
[ApiVersion("1.0")]
public class TaskOwnerReportsController : BaseController
{
    [HttpGet("task-owners-summary-reports/weekly")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ODataFilter(nameof(ApiWeeklyTaskOwnerReport.PeriodStart), nameof(ApiWeeklyTaskOwnerReport.PeriodEnd))]
    [ODataTop(100), ODataSkip]
    public async Task<ActionResult<ApiCollection<ApiWeeklyTaskOwnerReport>>> GetWeeklyTaskOwnerReportsV1(ODataQueryParams query)
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

        var reports = await DispatchAsync(new GetWeeklyTaskOwnerReports(query));

        return Ok(ApiCollection<ApiWeeklyTaskOwnerReport>.FromQueryCollection(reports, ApiWeeklyTaskOwnerReport.FromQueryWeeklyTaskOwnerReport));
    }

    [HttpGet("projects/{projectId:guid}/task-owners-summary-reports/weekly")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ODataFilter(nameof(ApiWeeklyTaskOwnerReport.PeriodStart), nameof(ApiWeeklyTaskOwnerReport.PeriodEnd))]
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

        var project = (await DispatchAsync(new GetProjects().WhereProjectId(projectId))).FirstOrDefault();
        if (project is null)
            return ProjectNotFound(projectId);

        var reports = await DispatchAsync(new GetWeeklyTaskOwnerReports(query).WhereProjectId(project.Id));

        return Ok(ApiCollection<ApiWeeklyTaskOwnerReport>.FromQueryCollection(reports, ApiWeeklyTaskOwnerReport.FromQueryWeeklyTaskOwnerReport));
    }

    [HttpGet("projects/{projectId:guid}/task-owners-summary-reports/weekly/{reportId:guid}")]
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

        var project = (await DispatchAsync(new GetProjects().WhereProjectId(projectId))).FirstOrDefault();
        if (project is null)
            return ProjectNotFound(projectId);

        var report = (await DispatchAsync(new GetWeeklyTaskOwnerReports()
                .WhereProjectId(project.Id)
                .WhereReportId(reportId)))
            .FirstOrDefault();

        return report is null ? NotFound() : Ok(ApiWeeklyTaskOwnerReport.FromQueryWeeklyTaskOwnerReport(report));
    }

    /// <summary>
    ///     Summary report key is composed of the project id and the period start and end dates.
    ///     If a report already exists for the given project id and period then it will be replaced.
    /// </summary>
    [HttpPut("projects/{projectId:guid}/task-owners-summary-reports/weekly")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiWeeklyTaskOwnerReport>> PutWeeklyTaskOwnerReportV1(Guid projectId, [FromBody] PutWeeklyTaskOwnerReportRequest request)
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

        var report = await DispatchAsync(command);

        return Ok(report);
    }
}