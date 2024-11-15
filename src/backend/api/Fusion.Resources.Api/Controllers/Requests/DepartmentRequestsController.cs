using Fusion.AspNetCore.FluentAuthorization;
using Fusion.AspNetCore.OData;
using Fusion.Authorization;
using Fusion.Integration.LineOrg;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace Fusion.Resources.Api.Controllers.Requests
{
    [ApiVersion("1.0-preview")]
    [ApiVersion("1.0")]
    [Authorize]
    [ApiController]
    public class DepartmentRequestsController : ResourceControllerBase
    {

        [EmulatedUserSupport]
        [HttpOptions("departments/{departmentString}/resources/requests")]
        public async Task<ActionResult<ApiCollection<ApiResourceAllocationRequest>>> OptionsDepartmentRequests([FromRoute] OrgUnitIdentifier departmentString)
        {
            if (!departmentString.Exists)
                return FusionApiError.NotFound(departmentString.OriginalIdentifier, "Could not locate department");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal().BeTrustedApplication();
                r.AnyOf(or =>
                {
                    or.BeResourceOwner(new DepartmentPath(departmentString.FullDepartment).GoToLevel(2), includeDescendants: true);
                    or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(departmentString.FullDepartment), AccessRoles.ResourceOwner);
                });
            });
            
            // Do not return 403, just dont return GET header

            #endregion

            var allowed = new List<string>();

            if (authResult.Success)
            {
                allowed.Add("GET");
            }

            Response.Headers.Append("Allow", string.Join(',', allowed));
            return NoContent();
        }


        [HttpGet("departments/{departmentString}/resources/requests")]
        public async Task<ActionResult<ApiCollection<ApiResourceAllocationRequest>>> GetDepartmentRequests(
            [FromRoute] OrgUnitIdentifier departmentString,
            [FromQuery] ODataQueryParams query)
        {
            if (!departmentString.Exists)
                return FusionApiError.NotFound(departmentString.OriginalIdentifier, "Could not locate department");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal().BeTrustedApplication();
                r.AnyOf(or =>
                {
                    or.BeResourceOwner(new DepartmentPath(departmentString.FullDepartment).GoToLevel(2), includeDescendants: true);
                    or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(departmentString.FullDepartment), AccessRoles.ResourceOwner);
                });
            });
            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var requestCommand = new GetResourceAllocationRequests(query)
                .ForResourceOwners()
                .WithAssignedDepartment(departmentString.FullDepartment);
            var result = await DispatchAsync(requestCommand);

            var apiModel = result.Select(x => new ApiResourceAllocationRequest(x)).ToList();
            return new ApiCollection<ApiResourceAllocationRequest>(apiModel);
        }


        [EmulatedUserSupport]
        [HttpGet("/departments/positions/{positionId}/requests")]
        public async Task<ActionResult<ApiCollection<ApiResourceAllocationRequest>>> OptionsRequestsForPosition(Guid positionId)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal().BeTrustedApplication();
                r.AnyOf(or =>
                {
                    or.BeResourceOwnerForAnyDepartment();
                    or.HaveAnyOrgUnitScopedRole(AccessRoles.ResourceOwner);
                });
            });

            #endregion

            var allowed = new List<string>();

            if (authResult.Success)
            {
                allowed.Add("GET");
            }

            Response.Headers.Append("Allow", string.Join(',', allowed));
            return NoContent();
        }

        /// <summary>
        /// List all requests that is relevant for a position. This endpoint is relevant for resource owners, and will include drafts that has not been sent to the task owners.
        /// 
        /// The endpoint only requires the user to be a resource owner to list requests. 
        /// This is due to the nature of knowing that a request exists is not restricted in itself. 
        /// However internal data like comments should not be exposed in this endpoint.
        /// 
        /// It is also difficult to determine a proper authorization rule, as requests could span multiple departments and sectors. The use case is 
        /// to get an overview of other requests for the position.
        /// </summary>
        /// <param name="positionId">The org position id</param>
        /// <param name="query">OData-like query params. Supports filtering and 1-level order</param>
        /// <returns></returns>
        [HttpGet("/departments/positions/{positionId}/requests")]
        public async Task<ActionResult<ApiCollection<ApiResourceAllocationRequest>>> GetRequestsForPosition(Guid positionId, [FromQuery] ODataQueryParams query)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal().BeTrustedApplication();
                r.AnyOf(or =>
                {
                    or.BeResourceOwnerForAnyDepartment();
                    or.HaveAnyOrgUnitScopedRole(AccessRoles.ResourceOwner);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();
            #endregion

            var command = new GetResourceAllocationRequests(query)
                .WithPositionId(positionId)
                .ForResourceOwners();

            var result = await DispatchAsync(command);

            return new ApiCollection<ApiResourceAllocationRequest>(result.Select(x => new ApiResourceAllocationRequest(x)));
        }

        [HttpGet("departments/{departmentString}/resources/requests/timeline")]
        public async Task<ActionResult<ApiRequestsTimeline>> GetDepartmentTimeline(
            [FromRoute] OrgUnitIdentifier departmentString,
            [FromQuery] ODataQueryParams query,
            [FromQuery] DateTime? timelineStart = null,
            [FromQuery] string? timelineDuration = null,
            [FromQuery] DateTime? timelineEnd = null)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    or.BeResourceOwnerForDepartment(new DepartmentPath(departmentString.FullDepartment).GoToLevel(2), includeDescendants: true);
                    or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(departmentString.FullDepartment), AccessRoles.ResourceOwner);
                });
            });
            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            #region validate timeline input

            if (timelineStart is null)
                return ApiErrors.MissingInput(nameof(timelineStart), "Must specify 'timelineStart'");

            TimeSpan? duration;

            try { duration = timelineDuration != null ? XmlConvert.ToTimeSpan("P5M") : null; }
            catch (Exception ex)
            {
                return ApiErrors.InvalidInput("Invalid duration value: " + ex.Message);
            }

            if (timelineEnd is null)
            {
                if (duration is null)
                    return ApiErrors.MissingInput(nameof(timelineDuration), "Must specify either 'timelineDuration' or 'timelineEnd' when expanding timeline");

                timelineEnd = timelineStart.Value.Add(duration.Value);
            }
            #endregion


            var requestCommand = new GetDepartmentRequestsTimeline(departmentString.FullDepartment, timelineStart.Value, timelineEnd.Value, query);
            var departmentRequestsTimeline = await DispatchAsync(requestCommand);

            var apiModel = new ApiRequestsTimeline(departmentRequestsTimeline, timelineStart.Value, timelineEnd.Value);

            return apiModel;
        }

        [HttpGet("departments/{departmentString}/resources/requests/unassigned")]
        public async Task<ActionResult<ApiCollection<ApiResourceAllocationRequest>>> GetDepartmentUnassignedRequests([FromRoute] OrgUnitIdentifier departmentString) 
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    or.BeResourceOwnerForDepartment(new DepartmentPath(departmentString.FullDepartment).GoToLevel(2), includeDescendants: true);
                    or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(departmentString.FullDepartment), AccessRoles.ResourceOwner);
                });
            });
            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion
            var countEnabled = Request.Query.ContainsKey("$count");

            var requestCommand = new GetDepartmentUnassignedRequests(departmentString.FullDepartment).WithOnlyCount(countEnabled);

            var result = await DispatchAsync(requestCommand);

            var apiModel = result.Select(x => new ApiResourceAllocationRequest(x)).ToList();
            return new ApiCollection<ApiResourceAllocationRequest>(apiModel)
            {
                TotalCount = countEnabled ? result.TotalCount : null
            };
        }

        [HttpGet("departments/{departmentString}/resources/requests/tbn")]
        public async Task<ActionResult> GetTBNPositions([FromRoute] OrgUnitIdentifier departmentString)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    or.BeResourceOwnerForDepartment(new DepartmentPath(departmentString.FullDepartment).GoToLevel(2), includeDescendants: true);
                    or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(departmentString.FullDepartment), AccessRoles.ResourceOwner);
                });
            });
            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var request = new GetTbnPositions(departmentString.FullDepartment);

            var data = await DispatchAsync(request);

            return Ok(data);
        }

        [HttpGet("departments/{departmentString}/resources/tbn-positions/timeline")]

        public async Task<ActionResult> GetTbnPositionsTimeline(
            [FromRoute] OrgUnitIdentifier departmentString,
            [FromQuery] DateTime? timelineStart = null,
            [FromQuery] string? timelineDuration = null,
            [FromQuery] DateTime? timelineEnd = null)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    or.BeResourceOwnerForDepartment(new DepartmentPath(departmentString.FullDepartment).GoToLevel(2), includeDescendants: true);
                    or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(departmentString.FullDepartment), AccessRoles.ResourceOwner);
                });
            });
            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            #region validate timeline input

            if (timelineStart is null)
                return ApiErrors.MissingInput(nameof(timelineStart), "Must specify 'timelineStart'");

            TimeSpan? duration;

            try { duration = timelineDuration != null ? XmlConvert.ToTimeSpan("P5M") : null; }
            catch (Exception ex)
            {
                return ApiErrors.InvalidInput("Invalid duration value: " + ex.Message);
            }

            if (timelineEnd is null)
            {
                if (duration is null)
                    return ApiErrors.MissingInput(nameof(timelineDuration), "Must specify either 'timelineDuration' or 'timelineEnd' when expanding timeline");

                timelineEnd = timelineStart.Value.Add(duration.Value);
            }
            #endregion

            var request = new GetTbnPositionsTimeline(departmentString.FullDepartment, timelineStart.Value, timelineEnd.Value);
            var timeline = await DispatchAsync(request);

            return Ok(new ApiTbnPositionTimeline(timeline, timelineStart.Value, timelineEnd.Value));
        }
    }
}
