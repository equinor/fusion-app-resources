using System.Net.Mime;
using Fusion.AspNetCore.FluentAuthorization;
using Fusion.AspNetCore.OData;
using Fusion.Summary.Api.Controllers.ApiModels;
using Fusion.Summary.Api.Controllers.Requests;
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

    public async Task<ActionResult<IEnumerable<ApiSummaryReport>>> GetSummaryReportsV1(
        [FromRoute] string sapDepartmentId, ODataQueryParams query)
    {
        #region Authorization

        // TODO:
        var authResult = await Request.RequireAuthorizationAsync(r => { r.AnyOf(or => { }); });

        if (authResult.Unauthorized)
            return authResult.CreateForbiddenResponse();

        #endregion

        // TODO: Query

        throw new NotImplementedException();
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
        var authResult = await Request.RequireAuthorizationAsync(r => { r.AnyOf(or => { }); });

        if (authResult.Unauthorized)
            return authResult.CreateForbiddenResponse();

        #endregion

        // TODO: Command

        throw new NotImplementedException();
    }
}