using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands.Departments;
using Fusion.Resources.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Controllers
{
    [ApiVersion("1.0-preview")]
    [ApiVersion("1.0")]
    [Authorize]
    [ApiController]
    public class DepartmentsController : ResourceControllerBase
    {
        private readonly IOrgApiClient orgApiClient;
        private readonly IRequestRouter requestRouter;

        public DepartmentsController(IOrgApiClientFactory orgApiClientFactory, IRequestRouter requestRouter)
        {
            this.orgApiClient = orgApiClientFactory.CreateClient(ApiClientMode.Application); ;
            this.requestRouter = requestRouter;
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

        [HttpGet("/departments/{departmentString}")]
        public async Task<ActionResult<ApiDepartment>> GetDepartments(string departmentString)
        {
            var department = await DispatchAsync(new GetDepartment(departmentString).ExpandDelegatedResourceOwners());
            if (department is null) return NotFound();

            return Ok(new ApiDepartment(department));
        }

        [HttpGet("/departments/{departmentString}/related")]
        public async Task<ActionResult<ApiRelatedDepartments>> GetRelevantDepartments(string departmentString)
        {
            var departments = await DispatchAsync(new GetRelatedDepartments(departmentString));
            if (departments is null) return NotFound();

            return Ok(new ApiRelatedDepartments(departments));
        }

        [HttpGet("/departments/{departmentString}/delegated-resource-owners")]
        public async Task<ActionResult<IEnumerable<ApiDepartmentResponsible>>> GetDelegatedDepartmentResponsiblesForDepartment(string departmentString)
        {
            var departmentResourceOwners = await DispatchAsync(new GetDelegatedDepartmentResponsibles(departmentString));
            return departmentResourceOwners.Select(x => new ApiDepartmentResponsible(x)).ToList();
        }

        [HttpPost("/departments/{departmentString}/delegated-resource-owner")]
        [HttpPost("/departments/{departmentString}/delegated-resource-owners")]
        public async Task<ActionResult<ApiDepartmentResponsible>> AddDelegatedResourceOwner(string departmentString, [FromBody] AddDelegatedResourceOwnerRequest request)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion Authorization

            var existingDepartment = await DispatchAsync(new GetDepartment(departmentString));
            if (existingDepartment is null) return NotFound();

            try
            {
                var command = new AddDelegatedResourceOwner(departmentString, request.ResponsibleAzureUniqueId)
                {
                    DateFrom = request.DateFrom,
                    DateTo = request.DateTo,
                    UpdatedByAzureUniqueId = User.GetAzureUniqueId() ?? User.GetApplicationId()
                }.WithReason(request.Reason);

                await DispatchAsync(command);

            }
            catch (RoleDelegationExistsError ex)
            {
                return FusionApiError.ResourceExists($"{request.ResponsibleAzureUniqueId}",
                    $"Person already delegated as resource owner for department '{departmentString}", ex);
            }
            var departmentResourceOwners =
                await DispatchAsync(new GetDelegatedDepartmentResponsibles(departmentString));
            var itemCreated = departmentResourceOwners.Select(x => new ApiDepartmentResponsible(x)).First(x =>
                x.DelegatedResponsible!.AzureUniquePersonId == request.ResponsibleAzureUniqueId);

            return CreatedAtAction(nameof(GetDepartments), new { departmentString }, itemCreated);

        }

        [HttpDelete("/departments/{departmentString}/delegated-resource-owner/{azureUniqueId}")]
        [HttpDelete("/departments/{departmentString}/delegated-resource-owners/{azureUniqueId}")]
        public async Task<IActionResult> DeleteDelegatedResourceOwner(string departmentString, Guid azureUniqueId)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion Authorization

            var deleted = await DispatchAsync(
                new DeleteDelegatedResourceOwner(departmentString, azureUniqueId)
            );

            if (deleted) return NoContent();
            else return NotFound();
        }

        [HttpGet("/projects/{projectId}/positions/{positionId}/instances/{instanceId}/relevant-departments")]
        public async Task<ActionResult<ApiRelevantDepartments>> GetPositionDepartments(
            Guid projectId, Guid positionId, Guid instanceId, CancellationToken cancellationToken)
        {
            var result = new ApiRelevantDepartments();

            var position = await orgApiClient.GetPositionV2Async(projectId, positionId);
            if (position is null) return NotFound();

            // Empty string is a valid department in line org (CEO), but we don't want to return that.
            if (string.IsNullOrWhiteSpace(position.BasePosition.Department)) return result;

            var routedDepartment = await requestRouter.RouteAsync(position, instanceId, cancellationToken);
            if (string.IsNullOrWhiteSpace(routedDepartment)) return result;

            var department = await DispatchAsync(new GetDepartment(routedDepartment).ExpandDelegatedResourceOwners());
            var related = await DispatchAsync(new GetRelatedDepartments(position.BasePosition.Department));

            if (related is not null)
            {
                result.Relevant.AddRange(
                    related.Siblings
                        .Union(related.Children)
                        .Select(x => new ApiDepartment(x))
                );
            }

            if (department is not null)
            {
                result.Department = new ApiDepartment(department);
                result.Relevant.Add(new ApiDepartment(department));
            }

            return result;
        }
    }
}