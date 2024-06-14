using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Summary.Api.Database.Entities;
using Fusion.Summary.Api.Services;
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
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;

    public DepartmentsController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

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
        // Add authorization
        #endregion Authorization

        var ret = new List<DepartmentTableEntity>();

        // Query
        var departments = await _departmentService.GetAllDepartments();

        if (departments.Count == 0)
            return NotFound();

        ret.AddRange(departments);

        // Return val
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
        // Add authorization
        #endregion Authorization

        var department = await _departmentService.GetDepartmentById(sapDepartmentId);

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
        // Add authorization
        #endregion Authorization

        var department = await _departmentService.GetDepartmentById(request.DepartmentSapId);

        // Check if department exist
        if (department == null)
        {
            await _departmentService.CreateDepartment(new DepartmentTableEntity
            {
                DepartmentSapId = request.DepartmentSapId,
                ResourceOwnerAzureUniqueId = request.ResourceOwnerAzureUniqueId,
                FullDepartmentName = request.FullDepartmentName
            });

            return Created();
        }
        // Check if department owner has changed
        else if (department.ResourceOwnerAzureUniqueId != request.ResourceOwnerAzureUniqueId)
        {
            await _departmentService.UpdateDepartment(request.DepartmentSapId, new DepartmentTableEntity
            {
                DepartmentSapId = request.DepartmentSapId,
                FullDepartmentName = request.FullDepartmentName,
                ResourceOwnerAzureUniqueId = request.ResourceOwnerAzureUniqueId
            });

            return Ok();
        }

        // No change
        return Ok();
    }
}
