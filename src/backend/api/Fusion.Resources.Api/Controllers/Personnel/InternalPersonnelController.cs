﻿using Fusion.AspNetCore.FluentAuthorization;
using Fusion.AspNetCore.OData;
using Fusion.Authorization;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace Fusion.Resources.Api.Controllers
{
    [ApiVersion("1.0-preview")]
    [Authorize]
    [ApiController]
    public class InternalPersonnelController : ResourceControllerBase
    {

        public InternalPersonnelController()
        {
        }
        /// <summary>
        /// Get personnel for a department
        /// </summary>
        /// <param name="fullDepartmentString">The department to retrieve personnel list from.</param>
        /// <param name="timelineStart">Start date of timeline</param>
        /// <param name="timelineDuration">Optional: duration of timeline i.e. P1M for 1 month</param>
        /// <param name="timelineEnd">Optional: specific end date of timeline</param>
        /// <param name="includeSubdepartments">Certain departments in line org exists where a 
        /// person in the department manages external users. Setting this flag to true will 
        /// include such personnel in the result.</param>
        /// <returns></returns>
        [HttpGet("departments/{fullDepartmentString}/resources/personnel")]
        public async Task<ActionResult<ApiCollection<ApiInternalPersonnelPerson>>> GetDepartmentPersonnel(string fullDepartmentString,
            [FromQuery] ODataQueryParams query,
            [FromQuery] DateTime? timelineStart = null,
            [FromQuery] string? timelineDuration = null,
            [FromQuery] DateTime? timelineEnd = null,
            [FromQuery] bool includeSubdepartments = false)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AnyOf(or =>
                {
                    or.BeTrustedApplication();
                    or.FullControl();

                    or.FullControlInternal();
                    or.BeResourceOwner(new DepartmentPath(fullDepartmentString).Parent(), includeParents: false, includeDescendants: true);
                    // - Fusion.Resources.Department.ReadAll in any department scope upwards in line org.
                });
                r.LimitedAccessWhen(x =>
                {
                    x.BeResourceOwner(new DepartmentPath(fullDepartmentString).GoToLevel(2), includeParents: false, includeDescendants: true);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion


            #region Validate input if timeline is expanded

            var shouldExpandTimeline = query.ShouldExpand("timeline");
            if (shouldExpandTimeline)
            {
                if (timelineStart is null)
                    return ApiErrors.MissingInput(nameof(timelineStart), "Must specify 'timelineStart' when expanding timeline");

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
            }

            #endregion

            var command = new GetDepartmentPersonnel(fullDepartmentString, query)
                .WithTimeline(shouldExpandTimeline, timelineStart, timelineEnd);
            command.IncludeSubdepartments(includeSubdepartments);

            var department = await DispatchAsync(command);


            var returnModel = department.Select(p => authResult.LimitedAuth
                ? ApiInternalPersonnelPerson.CreateWithoutConfidentialTaskInfo(p)
                : ApiInternalPersonnelPerson.CreateWithConfidentialTaskInfo(p)
            ).ToList();

            return new ApiCollection<ApiInternalPersonnelPerson>(returnModel);
        }

        [HttpGet("sectors/{sectorPath}/resources/personnel")]
        public async Task<ActionResult<ApiCollection<ApiInternalPersonnelPerson>>> GetSectorPersonnel(string sectorPath,
            [FromQuery] ODataQueryParams query,
            [FromQuery] DateTime? timelineStart = null,
            [FromQuery] string? timelineDuration = null,
            [FromQuery] DateTime? timelineEnd = null)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AnyOf(or =>
                {
                    or.BeTrustedApplication();
                    or.FullControl();

                    or.FullControlInternal();
                    or.BeResourceOwner(new DepartmentPath(sectorPath).Parent(), includeParents: false, includeDescendants: true);
                });
                r.LimitedAccessWhen(x =>
                {
                    x.BeResourceOwner(new DepartmentPath(sectorPath).GoToLevel(2), includeParents: false, includeDescendants: true);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion


            #region Validate input if timeline is expanded

            var shouldExpandTimeline = query.ShouldExpand("timeline");
            if (shouldExpandTimeline)
            {
                if (timelineStart is null)
                    return ApiErrors.MissingInput(nameof(timelineStart), "Must specify 'timelineStart' when expanding timeline");

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
            }

            #endregion


            var department = await DispatchAsync(new GetSectorPersonnel(sectorPath, query)
                .WithTimeline(shouldExpandTimeline, timelineStart, timelineEnd));

            var returnModel = department.Select(p => authResult.LimitedAuth
                ? ApiInternalPersonnelPerson.CreateWithoutConfidentialTaskInfo(p)
                : ApiInternalPersonnelPerson.CreateWithConfidentialTaskInfo(p)
            ).ToList();

            return new ApiCollection<ApiInternalPersonnelPerson>(returnModel);
        }

        [HttpGet("departments/{fullDepartmentString}/resources/personnel/{personIdentifier}")]
        public async Task<ActionResult<ApiInternalPersonnelPerson>> GetPersonnelAllocation(string fullDepartmentString, string personIdentifier)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AnyOf(or =>
                {
                    or.BeTrustedApplication();
                    or.FullControl();

                    or.FullControlInternal();

                    or.BeResourceOwner(new DepartmentPath(fullDepartmentString).Parent(), includeParents: false, includeDescendants: true);
                });
                r.LimitedAccessWhen(x =>
                {
                    x.BeResourceOwner(new DepartmentPath(fullDepartmentString).GoToLevel(2), includeParents: false, includeDescendants: true);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var personnelItem = await DispatchAsync(new GetPersonnelAllocation(personIdentifier));

            if (personnelItem is null)
                throw new InvalidOperationException("Could locate profile for person");

            if (personnelItem.FullDepartment != fullDepartmentString)
                return ApiErrors.NotFound($"Person does not belong to department ({personnelItem.FullDepartment})");

            var result = authResult.LimitedAuth
                ? ApiInternalPersonnelPerson.CreateWithoutConfidentialTaskInfo(personnelItem)
                : ApiInternalPersonnelPerson.CreateWithConfidentialTaskInfo(personnelItem);

            return Ok(result);
        }

        [HttpPost("departments/{fullDepartmentString}/resources/personnel/{personIdentifier}/allocations/{instanceId}/allocation-state/reset")]
        public async Task<ActionResult> ResetAllocationState(string fullDepartmentString, string personIdentifier, Guid instanceId)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AnyOf(or =>
                {
                    or.BeTrustedApplication();
                    or.FullControl();

                    or.FullControlInternal();

                    or.BeResourceOwner(new DepartmentPath(fullDepartmentString).GoToLevel(2), includeParents: false, includeDescendants: true);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var personnelItem = await DispatchAsync(new GetPersonnelAllocation(personIdentifier));

            if (personnelItem is null)
                throw new InvalidOperationException("Could locate profile for person");

            var allocation = personnelItem.PositionInstances.FirstOrDefault(i => i.InstanceId == instanceId);
            if (allocation is null)
                return ApiErrors.NotFound("Could not locate allocation on person");


            await DispatchAsync(new Domain.Commands.ResetAllocationState(allocation.Project.OrgProjectId, allocation.PositionId, instanceId));

            return NoContent();
        }

        [HttpGet("/projects/{projectIdentifier}/resources/persons")]
        public async Task<ActionResult> Search([FromRoute] PathProjectIdentifier projectIdentifier, [FromQuery] ODataQueryParams query)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    or.OrgChartReadAccess(projectIdentifier.ProjectId);
                    or.BeResourceOwner();
                });
            });


            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var command = new SearchPersonnel(query.Search);
            if(query.HasFilter)
            {
                var departmentFilter = query.Filter.GetFilterForField("department");
                if (departmentFilter.Operation != FilterOperation.Eq)
                    return BadRequest("Only the 'eq' operator is supported.");

                command = command.WithDepartmentFilter(departmentFilter.Value);
            }
            var result = await DispatchAsync(command);

            return Ok(result.Select(x => ApiInternalPersonnelPerson.CreateWithoutConfidentialTaskInfo(x)));
        }
    }

}
