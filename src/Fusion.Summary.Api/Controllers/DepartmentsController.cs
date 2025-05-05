using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Authorization;
using Fusion.Integration.Profile;
using Fusion.Summary.Api.Authorization.Extensions;
using Fusion.Summary.Api.Controllers.ApiModels;
using Fusion.Summary.Api.Controllers.Requests;
using Fusion.Summary.Api.Domain.Commands;
using Fusion.Summary.Api.Domain.Queries;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HttpRequestExtensions = Microsoft.ApplicationInsights.AspNetCore.Extensions.HttpRequestExtensions;

namespace Fusion.Summary.Api.Controllers;

[ApiVersion("1.0")]
[Authorize]
[ApiController]
public class DepartmentsController : BaseController
{
    [HttpGet("departments")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiDepartment[]>> GetDepartmentsV1()
    {
        #region Authorization

        var authResult = await Request.RequireAuthorizationAsync(r =>
        {
            r.AlwaysAccessWhen().ResourcesFullControl();
            r.AnyOf(or => { or.BeTrustedApplication(); });
        });

        if (authResult.Unauthorized)
            return authResult.CreateForbiddenResponse();

        #endregion Authorization

        var departments = await DispatchAsync(new GetAllDepartments());

        var apiDepartments = departments.Select(ApiDepartment.FromQueryDepartment);

        return Ok(apiDepartments);
    }

    [HttpGet("departments/{sapDepartmentId}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiDepartment>> GetDepartmentV1(string sapDepartmentId)
    {
        #region Authorization

        var authResult = await Request.RequireAuthorizationAsync(r =>
        {
            r.AlwaysAccessWhen().ResourcesFullControl();
            r.AnyOf(or => { or.BeTrustedApplication(); });
        });

        if (authResult.Unauthorized)
            return authResult.CreateForbiddenResponse();

        #endregion Authorization

        var department = await DispatchAsync(new GetDepartment(sapDepartmentId));

        // Check if department is null
        if (department == null)
            return DepartmentNotFound(sapDepartmentId);

        return Ok(ApiDepartment.FromQueryDepartment(department));
    }


    [HttpPut("departments/{sapDepartmentId}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PutV1(string sapDepartmentId, [FromBody] PutDepartmentRequest request)
    {
        #region Authorization

        var authResult = await Request.RequireAuthorizationAsync(r =>
        {
            r.AlwaysAccessWhen().ResourcesFullControl();
            r.AnyOf(or => { or.BeTrustedApplication(); });
        });

        if (authResult.Unauthorized)
            return authResult.CreateForbiddenResponse();

        #endregion Authorization

        var department = await DispatchAsync(new GetDepartment(sapDepartmentId));

        // Check if department exist
        if (department == null)
        {
            await DispatchAsync(
                new CreateDepartment(
                    sapDepartmentId,
                    request.FullDepartmentName,
                    request.ResourceOwnersAzureUniqueId,
                    request.DelegateResourceOwnersAzureUniqueId));

            return Created(Request.GetUri(), null);
        }

        // Check if department owners has changed
        if (!department.ResourceOwnersAzureUniqueId.SequenceEqual(request.ResourceOwnersAzureUniqueId) ||
            !department.DelegateResourceOwnersAzureUniqueId.SequenceEqual(request.DelegateResourceOwnersAzureUniqueId))
        {
            await DispatchAsync(
                new UpdateDepartment(
                    sapDepartmentId,
                    request.FullDepartmentName,
                    request.ResourceOwnersAzureUniqueId,
                    request.DelegateResourceOwnersAzureUniqueId));

            return NoContent();
        }

        // No change
        return NoContent();
    }
}