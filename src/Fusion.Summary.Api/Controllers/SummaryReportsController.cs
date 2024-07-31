using System.Net.Mime;
using Asp.Versioning;
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
[ApiVersion("1.0")]
public class SummaryReportsController : BaseController
{
    [HttpGet("resource-owners-summary-reports/{sapDepartmentId}/weekly")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ODataFilter(nameof(ApiWeeklySummaryReport.Period))]
    [ODataOrderBy(nameof(ApiWeeklySummaryReport.Period), nameof(ApiWeeklySummaryReport.Id))]
    [ODataTop(100), ODataSkip]
    [ApiVersion("1.0")]
    public async Task<ActionResult<ApiCollection<ApiWeeklySummaryReport>>> GetWeeklySummaryReportsV1(
        [FromRoute] string sapDepartmentId, ODataQueryParams query)
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

        if (string.IsNullOrWhiteSpace(sapDepartmentId))
            return BadRequest("SapDepartmentId route parameter is required");

        if (await DispatchAsync(new GetDepartment(sapDepartmentId)) is null)
            return DepartmentNotFound(sapDepartmentId);

        var queryReports = await DispatchAsync(new GetWeeklySummaryReports(sapDepartmentId, query));

        return Ok(ApiCollection<ApiWeeklySummaryReport>
            .FromQueryCollection(queryReports, ApiWeeklySummaryReport.FromQuerySummaryReport));
    }

    [HttpPut("resource-owners-summary-reports/{sapDepartmentId}/weekly")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ApiVersion("1.0")]
    public async Task<IActionResult> PutWeeklySummaryReportV1([FromRoute] string sapDepartmentId,
        [FromBody] PutWeeklySummaryReportRequest request)
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

        if (string.IsNullOrWhiteSpace(sapDepartmentId))
            return BadRequest("SapDepartmentId route parameter is required");

        if (await DispatchAsync(new GetDepartment(sapDepartmentId)) is null)
            return DepartmentNotFound(sapDepartmentId);

        if (request.Period.DayOfWeek != DayOfWeek.Monday)
            return BadRequest("Period date must be the first day of the week");

        var command = new PutWeeklySummaryReport(sapDepartmentId, request);

        await DispatchAsync(command);

        return NoContent();
    }
}