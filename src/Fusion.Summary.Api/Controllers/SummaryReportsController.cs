using System.Net.Mime;
using Fusion.AspNetCore.FluentAuthorization;
using Fusion.AspNetCore.OData;
using Fusion.Authorization;
using Fusion.Summary.Api.Authorization.Extensions;
using Fusion.Summary.Api.Controllers.ApiModels;
using Fusion.Summary.Api.Controllers.Requests;
using Fusion.Summary.Api.Domain.Commands;
using Fusion.Summary.Api.Domain.Models;
using Fusion.Summary.Api.Domain.Queries;
using Fusion.Summary.Api.Domain.Queries.Base;
using Microsoft.AspNetCore.Mvc;

namespace Fusion.Summary.Api.Controllers;

[ApiController]
// TODO: Add ApiVersion
public class SummaryReportsController : ControllerBase // TODO: Replace with custom base controller
{
    [HttpGet("summary-reports/{sapDepartmentId}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ODataFilter(nameof(ApiSummaryReport.Period), nameof(ApiSummaryReport.PositionsEnding),
        nameof(ApiSummaryReport.PersonnelMoreThan100PercentFTE), nameof(ApiSummaryReport.PeriodType))]
    [ODataOrderBy(nameof(ApiSummaryReport.Period), nameof(ApiSummaryReport.Id))]
    [ODataTop(1000), ODataSkip]
    public async Task<ActionResult<ApiCollection<ApiSummaryReport>>> GetSummaryReportsV1(
        [FromRoute] string sapDepartmentId, ODataQueryParams query)
    {
        #region Authorization

        // TODO:
        var authResult =
            await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();
                r.AnyOf(or => { or.BeTrustedApplication(); });
            });

        if (authResult.Unauthorized)
            return authResult.CreateForbiddenResponse();

        #endregion

        var queryRequest = new GetSummaryReport(sapDepartmentId, query);

        // TODO: Dispatch query

        QueryCollection<QuerySummaryReport> queryReports = null!;

        return Ok(new ApiCollection<QuerySummaryReport>(queryReports));
    }

    [HttpPut("summary-reports/{sapDepartmentId}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PutSummaryReportsV1([FromRoute] string sapDepartmentId,
        [FromBody] PutSummaryReportRequest request)
    {
        #region Authorization

        // TODO:
        var authResult =
            await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();
                r.AnyOf(or => { or.BeTrustedApplication(); });
            });

        if (authResult.Unauthorized)
            return authResult.CreateForbiddenResponse();

        #endregion

        // TODO: Command

        var command = new SetSummaryReport(sapDepartmentId, request);

        // TODO: Dispatch command

        return NoContent();
    }
}