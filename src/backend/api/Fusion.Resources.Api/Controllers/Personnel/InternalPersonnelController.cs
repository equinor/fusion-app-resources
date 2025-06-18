using Fusion.AspNetCore.FluentAuthorization;
using Fusion.AspNetCore.OData;
using Fusion.Authorization;
using Fusion.Integration.LineOrg;
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

    [Authorize]
    [ApiController]
    [ApiVersion("1.0-preview")]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    public partial class InternalPersonnelController : ResourceControllerBase
    {

        public InternalPersonnelController()
        {
        }

        [EmulatedUserSupport]
        [MapToApiVersion("1.0")]
        [MapToApiVersion("2.0")]
        [HttpOptions("departments/{departmentString}/resources/personnel")]
        public async Task<ActionResult<ApiCollection<ApiInternalPersonnelPerson>>> OptionsDepartmentPersonnel([FromRoute] OrgUnitIdentifier departmentString)
        {

            if (!departmentString.Exists)
                return FusionApiError.NotFound(departmentString.OriginalIdentifier, "Department does not exist");

            #region Authorization

            var sector = new DepartmentPath(departmentString.FullDepartment).Parent();
            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AnyOf(or =>
                {
                    or.BeTrustedApplication();
                    or.FullControl();

                    or.FullControlInternal();
                    or.BeResourceOwnerForDepartment(sector, includeParents: false, includeDescendants: true);
                    or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(departmentString.FullDepartment), AccessRoles.ResourceOwner);
                    // test
                    // - Fusion.Resources.Department.ReadAll in any department scope upwards in line org.
                });
                r.LimitedAccessWhen(x =>
                {
                    x.BeResourceOwnerForDepartment(new DepartmentPath(departmentString.FullDepartment).GoToLevel(2), includeParents: false, includeDescendants: true);
                });
            });

            #endregion


            if (authResult.Success)
            {
                Response.Headers["Allow"] = authResult.LimitedAuth ? "GET,LIMITED" : "GET";
            }

            return NoContent();
        }

        /// <summary>
        /// Get personnel for a department.
        /// 
        /// Version 2 only changes the data set returned, by utilizing a different query for employees.
        /// </summary>
        /// <param name="departmentString">The department to retrieve personnel list from. Either SAP id or full department path. Prefer SAP id</param>
        /// <param name="timelineStart">Start date of timeline</param>
        /// <param name="timelineDuration">Optional: duration of timeline i.e. P1M for 1 month</param>
        /// <param name="timelineEnd">Optional: specific end date of timeline</param>
        /// <param name="includeSubdepartments">Certain departments in line org exists where a 
        /// person in the department manages external users. Setting this flag to true will 
        /// include such personnel in the result.</param>
        /// <param name="includeCurrentAllocations">Optional: only include current allocation</param>
        /// <returns></returns>
        [MapToApiVersion("1.0")]
        [MapToApiVersion("2.0")]
        [HttpGet("departments/{departmentString}/resources/personnel")]
        public async Task<ActionResult<ApiCollection<ApiInternalPersonnelPerson>>> GetDepartmentPersonnel([FromRoute] OrgUnitIdentifier departmentString,
            [FromQuery] ODataQueryParams query,
            [FromQuery] DateTime? timelineStart = null,
            [FromQuery] string? timelineDuration = null,
            [FromQuery] DateTime? timelineEnd = null,
            [FromQuery] bool includeSubdepartments = false,
            [FromQuery] bool includeCurrentAllocations = false,
            int? version = 2)
        {

            if (!departmentString.Exists)
                return FusionApiError.NotFound(departmentString.OriginalIdentifier, "Department does not exist");

            #region Authorization

            var sector = new DepartmentPath(departmentString.FullDepartment).Parent();
            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AnyOf(or =>
                {
                    or.BeTrustedApplication();
                    or.FullControl();

                    or.FullControlInternal();
                    or.BeResourceOwnerForDepartment(sector, includeParents: false, includeDescendants: true);
                    or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(departmentString.FullDepartment), AccessRoles.ResourceOwner);
                    // - Fusion.Resources.Department.ReadAll in any department scope upwards in line org.
                });
                r.LimitedAccessWhen(x =>
                {
                    x.BeResourceOwnerForDepartment(new DepartmentPath(departmentString.FullDepartment).GoToLevel(2), includeParents: false, includeDescendants: true);
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

            var command = new GetDepartmentPersonnel(departmentString.FullDepartment, query)
                .IncludeSubdepartments(includeSubdepartments)
                .IncludeCurrentAllocations(includeCurrentAllocations)
                .WithTimeline(shouldExpandTimeline, timelineStart, timelineEnd);

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
                    or.BeResourceOwnerForDepartment(new DepartmentPath(sectorPath).Parent(), includeParents: false, includeDescendants: true);
                    or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(sectorPath), AccessRoles.ResourceOwner);
                });
                r.LimitedAccessWhen(x =>
                {
                    x.BeResourceOwnerForDepartment(new DepartmentPath(sectorPath).GoToLevel(2), includeParents: false, includeDescendants: true);
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

        [HttpGet("departments/{departmentString}/resources/personnel/{personIdentifier}")]
        public async Task<ActionResult<ApiInternalPersonnelPerson>> GetPersonnelAllocation([FromRoute] OrgUnitIdentifier departmentString, string personIdentifier, [FromQuery] bool includeCurrentAllocations = false)
        {
            // Should validate if department exists here as well, however cannot guarantee that this data is 100% consistent. Could cause 
            // endpoints to fail with no workaround. Might consider refactoring to reference the person without a department as part of the 
            // route, and calculate authorization based on the registered department on the user.

            #region Authorization
            var sector = new DepartmentPath(departmentString.FullDepartment).Parent();
            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AnyOf(or =>
                {
                    or.BeTrustedApplication();
                    or.FullControl();

                    or.FullControlInternal();

                    or.BeResourceOwnerForDepartment(sector, includeParents: false, includeDescendants: true);
                    or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(departmentString.FullDepartment), AccessRoles.ResourceOwner);
                });
                r.LimitedAccessWhen(x =>
                {
                    x.BeResourceOwnerForDepartment(new DepartmentPath(departmentString.FullDepartment).GoToLevel(2), includeParents: false, includeDescendants: true);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var personnelItem = await DispatchAsync(new GetPersonnelAllocation(personIdentifier)
                                                        .IncludeCurrentAllocations(includeCurrentAllocations));

            if (personnelItem is null)
                throw new InvalidOperationException("Could locate profile for person");

            if (personnelItem.FullDepartment != departmentString.FullDepartment)
                return ApiErrors.NotFound($"Person does not belong to department ({personnelItem.FullDepartment})");

            var result = authResult.LimitedAuth
                ? ApiInternalPersonnelPerson.CreateWithoutConfidentialTaskInfo(personnelItem)
                : ApiInternalPersonnelPerson.CreateWithConfidentialTaskInfo(personnelItem);

            return Ok(result);
        }
        [HttpPost("departments/{departmentString}/resources/personnel/{personIdentifier}/allocations/{instanceId}/allocation-state/reset")]
        public async Task<ActionResult> ResetAllocationState([FromRoute] OrgUnitIdentifier departmentString, string personIdentifier, Guid instanceId)
        {
            // Should add validation of department here as well, however we cannot guarantee that the user department is consistent with 
            // what the frontend use as params, ref endpoint above. Should consider allowing this operation outside of the 
            // department scope in the route. Rather use the users department as source for authorization.

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AnyOf(or =>
                {
                    or.BeTrustedApplication();
                    or.FullControl();

                    or.FullControlInternal();

                    or.BeResourceOwnerForDepartment(new DepartmentPath(departmentString.FullDepartment).GoToLevel(2), includeParents: false, includeDescendants: true);
                    or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(departmentString.FullDepartment), AccessRoles.ResourceOwner);
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


            await DispatchCommandAsync(new Domain.Commands.ResetAllocationState(allocation.Project.OrgProjectId, allocation.PositionId, instanceId));

            return NoContent();
        }

     
        [HttpGet("/departments/resources/persons")]
        [HttpGet("/projects/{projectIdentifier}/resources/persons")]
        public async Task<ActionResult<ApiCollection<ApiInternalPersonnelPerson>>> Search([FromRoute] PathProjectIdentifier? projectIdentifier, [FromQuery] ODataQueryParams query)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    if (projectIdentifier is not null)
                        or.OrgChartReadAccess(projectIdentifier.ProjectId);

                    or.BeResourceOwnerForAnyDepartment();
                    or.HaveAnyOrgUnitScopedRole(AccessRoles.ResourceOwner);
                });
            });


            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var command = new SearchPersonnel(query.Search)
                .WithPaging(query);

            if (query.HasFilter)
            {
                var departmentFilter = query.Filter.GetFilterForField("department");
                if (departmentFilter.Operation != FilterOperation.Eq)
                    return FusionApiError.InvalidOperation("InvalidQueryFilter", "Only the 'eq' operator is supported.");

                command = command.WithDepartmentFilter(departmentFilter.Value);
            }
            var result = await DispatchAsync(command);

            return new ApiCollection<ApiInternalPersonnelPerson>(result.Select(x => ApiInternalPersonnelPerson.CreateWithoutConfidentialTaskInfo(x)))
            {
                TotalCount = result.TotalCount
            };
        }

    }
}
