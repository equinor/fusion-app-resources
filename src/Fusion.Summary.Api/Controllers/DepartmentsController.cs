using Asp.Versioning;
using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Authorization;
using Fusion.Summary.Api.Domain.Commands;
using Fusion.Summary.Api.Domain.Queries;
using Fusion.Summary.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fusion.Summary.Api.Controllers;

public record PutDepartmentRquest(string DepartmentSapId, string FullDepartmentName, Guid ResourceOwnerAzureUniqueId);

public record GetDepartmentResponse(string departmentSapId, Guid resourceOwnerAzureUniqueId, string fullDepartmentName);

/// <summary>
/// TODO: Add summary
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[ApiController]
public class DepartmentsController : BaseController
{
    /// <summary>
    /// TODO: Add summary
    /// <returns></returns>
    [HttpGet("departments")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDepartmentsV1()
    {

        #region Authorization

        var authResult = await Request.RequireAuthorizationAsync(r =>
        {
            r.AlwaysAccessWhen().BeTrustedApplication();
        });

        if (authResult.Unauthorized)
            return authResult.CreateForbiddenResponse();

        #endregion Authorization

        var ret = new List<ApiDepartment>();

        // Query
        var departments = await DispatchAsync(new GetAllDepartments());

        if (departments.Count() == 0)
            return NotFound();
        else
        {
            foreach (var d in departments) ret.Add(ApiDepartment.FromQueryDepartment(d));
        }

        return Ok(ret);
    }

    /// <summary>
    /// TODO: Add summary
    /// <returns></returns>
    [HttpGet("departments/{sapDepartmentId}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDepartmentV1(string sapDepartmentId)
    {
        #region Authorization

        var authResult = await Request.RequireAuthorizationAsync(r =>
        {
            r.AlwaysAccessWhen().BeTrustedApplication();
        });

        if (authResult.Unauthorized)
            return authResult.CreateForbiddenResponse();

        #endregion Authorization

        var department = await DispatchAsync(new GetDepartment(sapDepartmentId));

        // Check if department is null
        if (department == null)
        {
            return NotFound();
        }

        return Ok(department);
    }

    /// <summary>
    /// TODO: Add summary
    /// <returns></returns>
    [HttpPut("departments/{sapDepartmentId}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PutV1(PutDepartmentRquest request)
    {
        #region Authorization

        var authResult = await Request.RequireAuthorizationAsync(r =>
        {
            r.AlwaysAccessWhen().BeTrustedApplication();
        });

        if (authResult.Unauthorized)
            return authResult.CreateForbiddenResponse();

        #endregion Authorization

        var department = await DispatchAsync(new GetDepartment(request.DepartmentSapId));

        // Check if department exist
        if (department == null)
        {
            await DispatchAsync(
                new CreateDepartment(
                    request.DepartmentSapId, 
                    request.ResourceOwnerAzureUniqueId, 
                    request.FullDepartmentName));

            return Created();
        }
        // Check if department owner has changed
        else if (department.ResourceOwnerAzureUniqueId != request.ResourceOwnerAzureUniqueId)
        {
            await DispatchAsync(
                new UpdateDepartment(
                    request.DepartmentSapId,
                    request.ResourceOwnerAzureUniqueId,
                    request.FullDepartmentName));

            return Ok();
        }

        // No change
        return Ok();
    }
}
