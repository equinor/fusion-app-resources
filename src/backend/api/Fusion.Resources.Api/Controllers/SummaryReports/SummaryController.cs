using System;
using System.Net;
using System.Threading.Tasks;
using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Authorization;
using Fusion.Integration.LineOrg;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Queries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Fusion.Resources.Api.Controllers.SummaryReports;

[ApiController]
public class SummaryController : ResourceControllerBase
{
    /// <summary>
    ///     Returns the latest summary report for the given department. If no report is found for the last week, 404 is
    ///     returned.
    /// </summary>
    [HttpGet("/departments/{orgUnit}/weekly-summaries/latest")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiSummaryReport?>> GetLatestSummaryReport([FromRoute] OrgUnitIdentifier orgUnit)
    {
        #region Authorization

        var authResult = await Request.RequireAuthorizationAsync(r =>
        {
            r.AnyOf(or =>
            {
                or.BeTrustedApplication();
                or.FullControl();

                or.FullControlInternal();
                or.BeResourceOwnerForDepartment(orgUnit.FullDepartment, includeParents: true, includeDescendants: false);
                or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(orgUnit.FullDepartment), AccessRoles.ResourceOwner);
            });
        });

        if (authResult.Unauthorized)
            return authResult.CreateForbiddenResponse();

        #endregion

        if (!orgUnit.Exists)
            return FusionApiError.NotFound(orgUnit.OriginalIdentifier, "Department not found");

        var latestSummaryReport = await DispatchAsync(GetSummaryReport.Latest(orgUnit.SapId));

        if (latestSummaryReport == null)
            return FusionApiError.NotFound(orgUnit.OriginalIdentifier, "No summary report found for the last week for the given department");

        return Ok(ApiSummaryReport.FromSummaryReportDto(latestSummaryReport));
    }


    /// <summary>
    ///     Get a specific summary report for the given department. periodStart query parameter is required.
    /// </summary>
    [HttpGet("/departments/{orgUnit}/weekly-summaries")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiCollection<ApiSummaryReport>>> GetSummaryReports([FromRoute] OrgUnitIdentifier orgUnit, [FromQuery] DateTime? periodStart)
    {
        #region Authorization

        var authResult = await Request.RequireAuthorizationAsync(r =>
        {
            r.AnyOf(or =>
            {
                or.BeTrustedApplication();
                or.FullControl();

                or.FullControlInternal();
                or.BeResourceOwnerForDepartment(orgUnit.FullDepartment, includeParents: true, includeDescendants: false);
                or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(orgUnit.FullDepartment), AccessRoles.ResourceOwner);
            });
        });

        if (authResult.Unauthorized)
            return authResult.CreateForbiddenResponse();

        #endregion

        if (!orgUnit.Exists)
            return FusionApiError.NotFound(orgUnit.OriginalIdentifier, "Department not found");

        if (periodStart == null || periodStart == DateTime.MinValue)
            return FusionApiError.InvalidOperation("periodStartIsRequired", "Query parameter periodStart is required");

        var summaryReport = await DispatchAsync(GetSummaryReport.ForPeriodStart(orgUnit.SapId, periodStart.Value));

        if (summaryReport == null)
            return FusionApiError.NotFound(orgUnit.OriginalIdentifier, "No summary report found for the given period and department");

        var result = new ApiCollection<ApiSummaryReport>([ApiSummaryReport.FromSummaryReportDto(summaryReport)]);

        return Ok(result);
    }
}