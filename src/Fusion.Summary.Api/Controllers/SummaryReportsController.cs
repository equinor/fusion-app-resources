using System.Net.Mime;
using Fusion.AspNetCore.FluentAuthorization;
using Fusion.AspNetCore.OData;
using Fusion.Summary.Api.Controllers.ApiModels;
using Microsoft.AspNetCore.Mvc;

namespace Fusion.Summary.Api.Controllers;

[ApiController]
// TODO: Add ApiVersion
public class SummaryReportsController : ControllerBase // TODO: Replace with custom base controller
{
    // TODO: Do we need more precise route?
    // OData params to filter out needed?
    [HttpGet("summary-reports/{sapDepartmentId}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ApiSummaryReport>>> GetSummaryReportsV1(
        [FromRoute] string sapDepartmentId, [FromQuery] ODataQueryParams query)
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
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PutSummaryReportsV1([FromRoute] string sapDepartmentId,
        [FromBody] ApiSummaryReport updatedReport)
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