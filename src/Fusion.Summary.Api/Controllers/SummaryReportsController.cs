using System.Net.Mime;
using Fusion.AspNetCore.Api;
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
[ApiVersion("1.0")]
public class SummaryReportsController : BaseController
{
    [HttpGet("resource-owners-summary-reports/{sapDepartmentId}/weekly")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ODataFilter(nameof(ApiWeeklySummaryReport.Period))]
    [ODataOrderBy(nameof(ApiWeeklySummaryReport.Period), nameof(ApiWeeklySummaryReport.Id))]
    [ODataTop(100), ODataSkip]
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
            return SapDepartmentIdRequired();

        if (await DispatchAsync(new GetDepartment(sapDepartmentId)) is null)
            return DepartmentNotFound(sapDepartmentId);

        var queryReports = await DispatchAsync(new GetWeeklySummaryReports(sapDepartmentId, query));

        return Ok(ApiCollection<ApiWeeklySummaryReport>
            .FromQueryCollection(queryReports, ApiWeeklySummaryReport.FromQuerySummaryReport));
    }

    /// <summary>
    ///     Summary report key is composed of the department sap id and the period date.
    ///     If a report already exists for the given period, it will be replaced.
    /// </summary>
    [HttpPut("resource-owners-summary-reports/{sapDepartmentId}/weekly")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
            return SapDepartmentIdRequired();

        if (await DispatchAsync(new GetDepartment(sapDepartmentId)) is null)
            return DepartmentNotFound(sapDepartmentId);

        if (request.Period.DayOfWeek != DayOfWeek.Monday)
            return FusionApiError.InvalidOperation("InvalidPeriod", "Weekly summary report period date must be a monday");

        var command = new PutWeeklySummaryReport(sapDepartmentId, request);

        var newReportCreated = await DispatchAsync(command);

        return newReportCreated ? Created(Request.GetUri(), null) : NoContent();
    }
}