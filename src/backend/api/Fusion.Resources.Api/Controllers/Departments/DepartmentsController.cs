using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Resources.Database;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands;
using Fusion.Resources.Domain.Commands.Departments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Controllers.Departments
{
    [ApiVersion("1.0-preview")]
    [Authorize]
    [ApiController]
    public class DepartmentsController : ResourceControllerBase
    {
        [HttpGet("/departments/{departmentString}")]
        public async Task<ActionResult<ApiDepartment>> GetDepartments(string departmentString)
        {
            var department = await DispatchAsync(new GetDepartment(departmentString));
            if (department is null) return NotFound();

            return Ok(new ApiDepartment(department));
        }

        [HttpGet("/departments")]
        public async Task<ActionResult<List<ApiDepartment>>> Search([FromQuery(Name = "$search")] string query)
        {
            var request = new GetDepartments()
                .ExpandDelegatedResourceOwners()
                .WhereResourceOwnerMatches(query);

            var result = await DispatchAsync(request);

            return Ok(result.Select(x => new ApiDepartment(x)));
        }

        [HttpPost("/departments")]
        public async Task<ActionResult<ApiDepartment>> AddDepartment(AddDepartmentRequest request)
        {
            #region Authorization
            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();
            #endregion

            var existingDepartment = await DispatchAsync(new GetDepartment(request.DepartmentId));
            if (existingDepartment is not null && existingDepartment.IsTracked) return Conflict();

            if (request.SectorId is not null)
            {
                var existingSector = await DispatchAsync(new GetDepartment(request.SectorId));
                if (existingSector is null) return BadRequest($"Cannot add department with parent {request.SectorId}. The parent does not exist");
                
                if (!existingSector.IsTracked)
                {
                    var addSector = new AddDepartment(existingSector.DepartmentId, null);
                    var newSector = await DispatchAsync(addSector);
                }
            }


            var command = new AddDepartment(request.DepartmentId, request.SectorId);

            var newDepartment = await DispatchAsync(command);

            return CreatedAtAction(
                nameof(GetDepartments),
                new { departmentString = newDepartment.DepartmentId },
                new ApiDepartment(newDepartment)
            );
        }

        [HttpPut("/departments/{departmentString}")]
        public async Task<ActionResult<ApiDepartment>> UpdateDepartment(string departmentString, UpdateDepartmentRequest request)
        {
            #region Authorization
            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();
            #endregion

            var existingDepartment = await DispatchAsync(new GetDepartment(departmentString));
            if (existingDepartment is null || !existingDepartment.IsTracked) return NotFound();

            var command = new UpdateDepartment(departmentString, request.SectorId);

            var newDepartment = await DispatchAsync(command);

            return Ok(new ApiDepartment(newDepartment));
        }

        [HttpPost("/departments/{departmentString}/delegated-resource-owner")]
        public async Task<ActionResult> AddDelegatedResourceOwner(string departmentString, [FromBody] AddDelegatedResourceOwnerRequest request)
        {
            #region Authorization
            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();
            #endregion

            var existingDepartment = await DispatchAsync(new GetDepartment(departmentString));
            if (existingDepartment is null || !existingDepartment.IsTracked) return NotFound();

            var command = new AddDelegatedResourceOwner(departmentString, request.ResponsibleAzureUniqueId)
            {
                DateFrom = request.DateFrom,
                DateTo = request.DateTo
            };

            await DispatchAsync(command);


            return CreatedAtAction(nameof(GetDepartments), new { departmentString }, null);
        }
    }
}
