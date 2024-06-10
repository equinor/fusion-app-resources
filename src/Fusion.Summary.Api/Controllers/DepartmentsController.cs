using Fusion.Summary.Api.Database.Entities;
using Fusion.Summary.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fusion.Summary.Api.Controllers;

public record PutDepartmentRquest(string DepartmentSapId, string FullDepartmentName, Guid ResourceOwnerAzureUniqueId);

public record GetDepartmentResponse(string departmentSapId, Guid resourceOwnerAzureUniqueId, string fullDepartmentName);

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;

    public DepartmentsController(IDepartmentService departmentService) 
    {
        _departmentService = departmentService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<DepartmentTableEntity>), 200)]
    [ProducesResponseType(typeof(string), 401)]
    [ProducesResponseType(typeof(string), 403)]
    [ProducesResponseType(typeof(string), 404)]
    public async Task<IActionResult> GetDepartmentsV1()
    {
        var ret = new List<DepartmentTableEntity>();

        // Query
        var departments = await _departmentService.GetAllDepartments();

        if (departments.Count == 0) return NotFound();

        ret.AddRange(departments);

        // Return val
        return Ok(ret);
    }

    [HttpGet("{sapDepartmentId}")]
    [ProducesResponseType(typeof(DepartmentTableEntity), 200)]
    [ProducesResponseType(typeof(string), 401)]
    [ProducesResponseType(typeof(string), 403)]
    [ProducesResponseType(typeof(string), 404)]
    public async Task<IActionResult> GetDepartmentV1(string sapDepartmentId)
    {
        var department = await _departmentService.GetDepartmentById(sapDepartmentId);

        // Check if department is null
        if( department == null )
        {
            return NotFound();
        }

        return Ok(department);
    }

    [HttpPut]
    [ProducesResponseType(typeof(string), 200)]
    [ProducesResponseType(typeof(string), 201)]
    [ProducesResponseType(typeof(string), 401)]
    [ProducesResponseType(typeof(string), 403)]
    [ProducesResponseType(typeof(string), 404)]
    public async Task<IActionResult> PutV1(PutDepartmentRquest request)
    {
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
