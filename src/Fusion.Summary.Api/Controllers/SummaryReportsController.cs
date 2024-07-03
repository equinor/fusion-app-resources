using System.Net.Mime;
using Asp.Versioning;
using Fusion.AspNetCore.FluentAuthorization;
using Fusion.AspNetCore.OData;
using Fusion.Authorization;
using Fusion.Summary.Api.Authorization.Extensions;
using Fusion.Summary.Api.Controllers.ApiModels;
using Fusion.Summary.Api.Controllers.Requests;
using Fusion.Summary.Api.Domain.Commands;
using Fusion.Summary.Api.Domain.Models;
using Fusion.Summary.Api.Domain.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fusion.Summary.Api.Controllers;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
public class SummaryReportsController : BaseController
{
    [HttpGet("summary-reports/{sapDepartmentId}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ODataFilter(nameof(ApiSummaryReport.Period), nameof(ApiSummaryReport.PositionsEnding),
        nameof(ApiSummaryReport.PersonnelMoreThan100PercentFTE), nameof(ApiSummaryReport.PeriodType))]
    [ODataOrderBy(nameof(ApiSummaryReport.Period), nameof(ApiSummaryReport.Id))]
    [ODataTop(1000), ODataSkip]
    [ApiVersion("1.0")]
    public async Task<ActionResult<ApiCollection<ApiSummaryReport>>> GetSummaryReportsV1(
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
            return NotFound();

        var queryReports = await DispatchAsync(new GetSummaryReports(sapDepartmentId, query));

        return Ok(new ApiCollection<QuerySummaryReport>(queryReports));
    }

    [HttpPut("summary-reports/{sapDepartmentId}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ApiVersion("1.0")]
    public async Task<IActionResult> PutSummaryReportsV1([FromRoute] string sapDepartmentId,
        [FromBody] PutSummaryReportRequest request)
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
            return NotFound();

        var command = new PutSummaryReport(sapDepartmentId, request);

        await DispatchAsync(command);

        return NoContent();
    }
}