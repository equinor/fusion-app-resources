using Fusion.AspNetCore.FluentAuthorization;
using Fusion.AspNetCore.OData;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace Fusion.Resources.Api.Controllers.Requests
{
    [ApiVersion("1.0-preview")]
    [Authorize]
    [ApiController]
    public class DepartmentRequestsController : ResourceControllerBase
    {
        [HttpGet("departments/{departmentString}/resources/requests")]
        public async Task<ActionResult<ApiCollection<ApiResourceAllocationRequest>>> GetDepartmentRequests(
            [FromRoute] string departmentString, 
            [FromQuery] ODataQueryParams query)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    // add requirements
                });
            });
            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var requestCommand = new GetResourceAllocationRequests(query)
                .ForResourceOwners()
                .WithAssignedDepartment(departmentString);
            var result = await DispatchAsync(requestCommand);

            var apiModel = result.Select(x => new ApiResourceAllocationRequest(x)).ToList();
            return new ApiCollection<ApiResourceAllocationRequest>(apiModel);
        }

        [HttpGet("departments/{departmentString}/resources/requests/timeline")]
        public async Task<ActionResult<ApiRequestsTimeline>> GetDepartmentTimeline(
            [FromRoute] string departmentString,
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
                    // add requirements
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


            var requestCommand = new GetDepartmentRequestsTimeline(departmentString, timelineStart.Value, timelineEnd.Value, query);
            var departmentRequestsTimeline = await DispatchAsync(requestCommand);

            var apiModel = new ApiRequestsTimeline(departmentRequestsTimeline, timelineStart.Value, timelineEnd.Value);

            return apiModel;
        }

        [HttpGet("departments/{departmentString}/resources/requests/unassigned")]
        public async Task<ActionResult<ApiCollection<ApiResourceAllocationRequest>>> GetDepartmentUnassignedRequests(string departmentString) 
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    // add requirements
                });
            });
            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var requestCommand = new GetDepartmentUnassignedRequests(departmentString);
            var result = await DispatchAsync(requestCommand);

            var apiModel = result.Select(x => new ApiResourceAllocationRequest(x)).ToList();
            return new ApiCollection<ApiResourceAllocationRequest>(apiModel);
        }

        [HttpGet("departments/{departmentString}/resources/requests/tbn")]
        public async Task<ActionResult> GetTBNPositions(string departmentString)
        {
            var request = new GetTBNPositions(departmentString);

            var data = await DispatchAsync(request);

            return Ok(data);
        }
    }
}
