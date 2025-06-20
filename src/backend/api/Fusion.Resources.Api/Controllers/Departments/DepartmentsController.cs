using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Authorization;
using Fusion.Integration.LineOrg;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands.Departments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Fusion.Resources.Api.Controllers
{
    [ApiVersion("1.0-preview")]
    [ApiVersion("1.0")]
    [ApiVersion("1.1")]
    [Authorize]
    [ApiController]
    public class DepartmentsController : ResourceControllerBase
    {
        private readonly IRequestRouter requestRouter;

        public DepartmentsController(IRequestRouter requestRouter)
        {
            this.requestRouter = requestRouter;
        }

        [HttpGet("/departments")]
        [MapToApiVersion("1.0-preview")]
        [MapToApiVersion("1.0")]
        public async Task<ActionResult<List<ApiDepartment>>> Search([FromQuery(Name = "$search")] string query)
        {
            var request = new GetDepartments()
                .ExpandDelegatedResourceOwners()
                .WhereResourceOwnerMatches(query);

            var result = await DispatchAsync(request);

            return Ok(result.Select(x => new ApiDepartment(x)));
        }

        [HttpGet("/departments/{departmentString}")]
        [MapToApiVersion("1.0-preview")]
        [MapToApiVersion("1.0")]
        public async Task<ActionResult<ApiDepartment>> GetDepartments([FromRoute] OrgUnitIdentifier departmentString)
        {
            if (!departmentString.Exists)
                return FusionApiError.NotFound(departmentString.OriginalIdentifier, "Department not found");

            var department = await DispatchAsync(new GetDepartment(departmentString.SapId).ExpandDelegatedResourceOwners());

            return Ok(new ApiDepartment(department!));
        }

        /// <summary>
        ///     This endpoint compared to 1.0 has authorization for the department.
        ///     This can be used to check if the user has access to the org-unit.
        /// </summary>
        [HttpGet("/departments/{departmentString}")]
        [MapToApiVersion("1.1")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiDepartment>> GetDepartmentsV11([FromRoute] OrgUnitIdentifier departmentString)
        {
            if (!departmentString.Exists)
                return FusionApiError.NotFound(departmentString.OriginalIdentifier, "Department not found");

            #region Authorization

            var departmentPath = new DepartmentPath(departmentString.FullDepartment);
            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal().BeTrustedApplication();
                r.AnyOf(or =>
                {
                    or.BeResourceOwnerForDepartment(departmentPath);
                    or.BeResourceOwnerForDepartment(departmentPath.Parent());
                    or.BeSiblingResourceOwner(departmentPath);
                    or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(departmentString.FullDepartment), AccessRoles.ResourceOwner);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion Authorization

            var department = await DispatchAsync(new GetDepartment(departmentString.SapId).ExpandDelegatedResourceOwners());

            return Ok(new ApiDepartment(department!));
        }

        [HttpOptions("/departments/{departmentString}")]
        [MapToApiVersion("1.1")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> GetDepartmentAccessV11([FromRoute] OrgUnitIdentifier departmentString)
        {
            if (!departmentString.Exists)
                return FusionApiError.NotFound(departmentString.OriginalIdentifier, "Department not found");

            #region Authorization

            var departmentPath = new DepartmentPath(departmentString.FullDepartment);
            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal().BeTrustedApplication();
                r.AnyOf(or =>
                {
                    or.BeResourceOwnerForDepartment(departmentPath);
                    or.BeResourceOwnerForDepartment(departmentPath.Parent());
                    or.BeSiblingResourceOwner(departmentPath);
                    or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(departmentString.FullDepartment), AccessRoles.ResourceOwner);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion Authorization


            var allowedMethods = new List<string>();
            if (authResult.Success)
            {
                allowedMethods.Add("GET");
                allowedMethods.Add("PUT");
            }

            Response.Headers["Allow"] = string.Join(',', allowedMethods);

            return NoContent();
        }

        [HttpGet("/departments/{departmentString}/related")]
        [MapToApiVersion("1.0-preview")]
        [MapToApiVersion("1.0")]
        public async Task<ActionResult<ApiRelatedDepartments>> GetRelevantDepartments([FromRoute] OrgUnitIdentifier departmentString)
        {
            if (!departmentString.Exists)
                return FusionApiError.NotFound(departmentString.OriginalIdentifier, "Department not found");

            var departments = await DispatchAsync(new GetRelatedDepartments(departmentString.SapId));

            return Ok(new ApiRelatedDepartments(departments!));
        }

        [HttpOptions("/departments/{departmentString}/delegated-resource-owners")]
        [MapToApiVersion("1.0-preview")]
        [MapToApiVersion("1.0")]
        [EmulatedUserSupport]
        public async Task<ActionResult> GetDelegatedResourceOwnersOptions([FromRoute] OrgUnitIdentifier departmentString)
        {
            if (!departmentString.Exists)
                return FusionApiError.NotFound(departmentString.OriginalIdentifier, "Department not found");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    or.CanDelegateAccessToDepartment(new DepartmentPath(departmentString.FullDepartment));

                });
                r.LimitedAccessWhen(or =>
                {
                    or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(departmentString.FullDepartment), AccessRoles.ResourceOwner);
                    or.BeEmployee();
                    or.BeConsultant();
                });
            });

            #endregion Authorization

            var allowedMethods = new List<string> { "OPTIONS" };

            if (authResult.Success)
            {
                if (authResult.LimitedAuth == false)
                {
                    allowedMethods.Add("DELETE");
                    allowedMethods.Add("POST");
                }
                allowedMethods.Add("GET");
            }

            Response.Headers["Allow"] = string.Join(',', allowedMethods);
            return NoContent();
        }
        
        [HttpGet("/departments/{departmentString}/delegated-resource-owners")]
        [MapToApiVersion("1.0-preview")]
        [MapToApiVersion("1.0")]
        public async Task<ActionResult<IEnumerable<ApiDepartmentResponsible>>> GetDelegatedDepartmentResponsiblesForDepartment([FromRoute] OrgUnitIdentifier departmentString, bool shouldIgnoreDateFilter)
        {
            if (!departmentString.Exists)
                return FusionApiError.NotFound(departmentString.OriginalIdentifier, "Department not found");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal().BeTrustedApplication();
                r.AnyOf(or =>
                {
                    or.CanDelegateAccessToDepartment(new DepartmentPath(departmentString.FullDepartment));
                    or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(departmentString.FullDepartment), AccessRoles.ResourceOwner);
                });
                r.LimitedAccessWhen(or =>
                {
                    or.BeEmployee();
                    or.BeConsultant();

                });
            });

            #endregion Authorization

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            var departmentResourceOwners = await DispatchAsync(new GetDelegatedDepartmentResponsibles(departmentString).IgnoreDateFilter(shouldIgnoreDateFilter));
            if (authResult.LimitedAuth)
            {
                return departmentResourceOwners.Select(x => ApiDepartmentResponsible.CreateLimitedDelegatedResponsible(x)).ToList();
            }

            return departmentResourceOwners.Select(x => new ApiDepartmentResponsible(x)).ToList();
        }

        [HttpPost("/departments/{departmentString}/delegated-resource-owner")]
        [HttpPost("/departments/{departmentString}/delegated-resource-owners")]
        [MapToApiVersion("1.0-preview")]
        [MapToApiVersion("1.0")]
        public async Task<ActionResult<ApiDepartmentResponsible>> AddDelegatedResourceOwner([FromRoute] OrgUnitIdentifier departmentString, [FromBody] AddDelegatedResourceOwnerRequest request)
        {
            if (!departmentString.Exists)
                return FusionApiError.NotFound(departmentString.OriginalIdentifier, "Department not found");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    or.CanDelegateAccessToDepartment(new DepartmentPath(departmentString.FullDepartment));
                });

            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion Authorization

            try
            {
                var command = new AddDelegatedResourceOwner(departmentString, request.ResponsibleAzureUniqueId)
                {
                    DateFrom = request.DateFrom,
                    DateTo = request.DateTo,
                    UpdatedByAzureUniqueId = User.GetAzureUniqueId() ?? User.GetApplicationId()
                }.WithReason(request.Reason);

                await DispatchCommandAsync(command);

            }
            catch (RoleDelegationExistsError ex)
            {
                return FusionApiError.ResourceExists($"{request.ResponsibleAzureUniqueId}",
                    $"Person already delegated as resource owner for department '{departmentString}", ex);
            }
            var departmentResourceOwners = await DispatchAsync(new GetDelegatedDepartmentResponsibles(departmentString).IgnoreDateFilter());

            var itemCreated = departmentResourceOwners.Select(x => new ApiDepartmentResponsible(x)).First(x =>
                x.DelegatedResponsible!.AzureUniquePersonId == request.ResponsibleAzureUniqueId);

            return CreatedAtAction(nameof(GetDepartments), new { departmentString }, itemCreated);

        }

        [HttpDelete("/departments/{departmentString}/delegated-resource-owner/{azureUniqueId}")]
        [HttpDelete("/departments/{departmentString}/delegated-resource-owners/{azureUniqueId}")]
        [MapToApiVersion("1.0-preview")]
        [MapToApiVersion("1.0")]
        public async Task<IActionResult> DeleteDelegatedResourceOwner([FromRoute] OrgUnitIdentifier departmentString, Guid azureUniqueId)
        {
            if (!departmentString.Exists)
                return FusionApiError.NotFound(departmentString.OriginalIdentifier, "Department not found");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    or.CanDelegateAccessToDepartment(new DepartmentPath(departmentString.FullDepartment));
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion Authorization

            var deleted = await DispatchAsync(new DeleteDelegatedResourceOwner(departmentString.FullDepartment, azureUniqueId));
            
            return NoContent();
        }

        [HttpGet("/projects/{projectId}/positions/{positionId}/instances/{instanceId}/relevant-departments")]
        [MapToApiVersion("1.0-preview")]
        [MapToApiVersion("1.0")]
        public async Task<ActionResult<ApiRelevantDepartments>> GetPositionDepartments(
            Guid projectId, Guid positionId, Guid instanceId, CancellationToken cancellationToken)
        {
            var result = new ApiRelevantDepartments();

            var position = await ResolvePositionAsync(positionId);
            if (position is null || position.ProjectId != projectId)
                return FusionApiError.NotFound(positionId, "Could not locate position");

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
