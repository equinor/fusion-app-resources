using Fusion.AspNetCore.FluentAuthorization;
using Fusion.AspNetCore.OData;
using Fusion.Authorization;
using Fusion.Integration.LineOrg;
using Fusion.Summary.Api.Authorization;
using Fusion.Summary.Api.Authorization.Extensions;
using Fusion.Summary.Api.Controllers.ApiModels;
using Fusion.Summary.Api.Controllers.Filter;
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
public class ResourceOwnerReportsController : BaseController
{
    [HttpGet("resource-owners-summary-reports/{sapDepartmentId}/weekly/metadata")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ODataTop(100), ODataSkip]
    public async Task<ActionResult<ApiCollection<ApiReportMetaData>>> GetWeeklySummaryReportsMetaData(
        [FromRoute] string sapDepartmentId, [FromQuery] ODataQueryParams query)
    {
        #region Authorization

        var authResult =
            await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().ResourcesFullControl();
                r.AnyOf(or =>
                {
                    or.BeTrustedApplication();
                    or.HaveOrgUnitScopedRole(DepartmentId.FromSapId(sapDepartmentId), AccessRoles.ResourceOwnerRoles);
                });
            });

        if (authResult.Unauthorized)
            return authResult.CreateForbiddenResponse();

        #endregion

        if (await DispatchAsync(new GetDepartment(sapDepartmentId)) is null)
            return DepartmentNotFound(sapDepartmentId);

        var queryReports = await DispatchAsync(new GetWeeklySummaryReportsMetaData(sapDepartmentId, query));

        return Ok(ApiCollection<ApiReportMetaData>
            .FromQueryCollection(queryReports, ApiReportMetaData.FromQueryReportMetaData));
    }

    [HttpGet("resource-owners-summary-reports/{sapDepartmentId}/weekly")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
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
                r.AnyOf(or =>
                {
                    or.BeTrustedApplication();
                    or.HaveOrgUnitScopedRole(DepartmentId.FromSapId(sapDepartmentId), AccessRoles.ResourceOwnerRoles);
                });
            });

        if (authResult.Unauthorized)
            return authResult.CreateForbiddenResponse();

        #endregion

        if (await DispatchAsync(new GetDepartment(sapDepartmentId)) is null)
            return DepartmentNotFound(sapDepartmentId);

        var queryReports = await DispatchAsync(new GetWeeklySummaryReports(sapDepartmentId, query));

        return Ok(ApiCollection<ApiWeeklySummaryReport>
            .FromQueryCollection(queryReports, ApiWeeklySummaryReport.FromQuerySummaryReport));
    }

    [HttpGet("resource-owners-summary-reports/{sapDepartmentId}/weekly/{reportId:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiWeeklySummaryReport>> GetWeeklySummaryReportByIdV1(
        [FromRoute] string sapDepartmentId, [FromRoute] Guid reportId)
    {
        #region Authorization

        var authResult =
            await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().ResourcesFullControl();
                r.AnyOf(or =>
                {
                    or.BeTrustedApplication();
                    or.HaveOrgUnitScopedRole(DepartmentId.FromSapId(sapDepartmentId), AccessRoles.ResourceOwnerRoles);
                });
            });

        if (authResult.Unauthorized)
            return authResult.CreateForbiddenResponse();

        #endregion

        if (await DispatchAsync(new GetDepartment(sapDepartmentId)) is null)
            return DepartmentNotFound(sapDepartmentId);

        var report = await DispatchAsync(new GetWeeklySummaryReport(sapDepartmentId, reportId));

        if (report is null)
            return FusionApiError.NotFound(reportId, "Weekly summary report not found");

        return Ok(ApiWeeklySummaryReport.FromQuerySummaryReport(report));
    }

    /// <summary>
    ///     Gets the latest weekly summary report for the given department.
    ///     404 is returned if no report has been created for the current week
    /// </summary>
    [HttpGet("resource-owners-summary-reports/{sapDepartmentId}/weekly/latest")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiWeeklySummaryReport>> GetLatestWeeklySummaryReportV1(
        [FromRoute] string sapDepartmentId)
    {
        #region Authorization

        var authResult =
            await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().ResourcesFullControl();
                r.AnyOf(or =>
                {
                    or.BeTrustedApplication();
                    or.HaveOrgUnitScopedRole(DepartmentId.FromSapId(sapDepartmentId), AccessRoles.ResourceOwnerRoles);
                });
            });

        if (authResult.Unauthorized)
            return authResult.CreateForbiddenResponse();

        #endregion

        if (await DispatchAsync(new GetDepartment(sapDepartmentId)) is null)
            return DepartmentNotFound(sapDepartmentId);

        var report = await DispatchAsync(GetWeeklySummaryReport.Latest(sapDepartmentId));

        if (report is null)
            return FusionApiError.NotFound("latest", "Weekly summary report not found");

        return Ok(ApiWeeklySummaryReport.FromQuerySummaryReport(report));
    }

    [HttpOptions("resource-owners-summary-reports/{sapDepartmentId}/weekly")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EmulatedUserSupport]
    public async Task<IActionResult> OptionsWeeklySummary([FromRoute] string sapDepartmentId, [FromQuery] string? emulatedUserId)
    {
        var authResult =
            await Request.RequireAuthorizationAsync(r =>
            {
                r.AnyOf(or =>
                {
                    or.BeTrustedApplication();
                    or.ResourcesFullControl();
                });
                r.LimitedAccessWhen(or => { or.HaveOrgUnitScopedRole(DepartmentId.FromSapId(sapDepartmentId), AccessRoles.ResourceOwnerRoles); });
            });

        var headers = new HashSet<HttpMethod>();

        if (authResult.LimitedAuth)
        {
            headers.Add(HttpMethod.Get);
        }
        else if (authResult.Success)
        {
            headers.Add(HttpMethod.Get);
            headers.Add(HttpMethod.Put);
        }

        Response.Headers.Allow = string.Join(",", headers);
        return NoContent();
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

        if (await DispatchAsync(new GetDepartment(sapDepartmentId)) is null)
            return DepartmentNotFound(sapDepartmentId);

        if (request.Period.DayOfWeek != DayOfWeek.Monday)
            return FusionApiError.InvalidOperation("InvalidPeriod", "Weekly summary report period date must be a monday");

        var command = new PutWeeklySummaryReport(sapDepartmentId, request);

        var newReportCreated = await DispatchAsync(command);

        return newReportCreated ? Created(Request.GetUri(), null) : NoContent();
    }
}