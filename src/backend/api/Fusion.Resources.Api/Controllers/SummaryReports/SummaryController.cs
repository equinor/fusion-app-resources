using System;
using System.Linq;
using System.Threading.Tasks;
using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Authorization;
using Fusion.Integration.LineOrg;
using Fusion.Resources.Application.SummaryClient.Models;
using Fusion.Resources.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Fusion.Resources.Api.Controllers.SummaryReports;

[ApiController]
public class SummaryController : ResourceControllerBase
{
    /// <summary>
    ///     Get weekly summary reports for a department. If period is specified it will resolve to the latest report for that date.
    ///     If the date is a monday it will return the report generated for that week.
    /// </summary>
    [HttpGet("/departments/{orgUnit}/weekly-summaries")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiCollection<ApiSummaryReport>>> GetSummaryReports([FromRoute] OrgUnitIdentifier orgUnit,
        [FromQuery] int? top, [FromQuery] int? skip, [FromQuery] DateTime? period)
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

        if (top < 0 || skip < 0)
            return FusionApiError.InvalidOperation("InvalidTopSkip", "Top and Skip values must be non-negative");

        // There should only be a single report for a given period so can simply ignore them if period is specified
        var query = period != null
            ? Domain.Queries.GetSummaryReports.ForPeriodStart(orgUnit.SapId, period.Value)
            : Domain.Queries.GetSummaryReports.GetWithTopAndSkip(orgUnit.SapId, top: top, skip: skip);


        var result = await DispatchAsync(query);

        return Ok(MapSummaryCollectionDto(result));


        ApiCollection<ApiSummaryReport> MapSummaryCollectionDto(SummaryApiCollectionDto<ResourceOwnerWeeklySummaryReportDto> collectionDto)
        {
            var summaryReports = collectionDto.Items.Select(ApiSummaryReport.FromSummaryReportDto).ToArray();
            return new ApiCollection<ApiSummaryReport>(summaryReports)
            {
                TotalCount = collectionDto.TotalCount,
                Top = collectionDto.Top,
                Skip = collectionDto.Skip
            };
        }
    }
}