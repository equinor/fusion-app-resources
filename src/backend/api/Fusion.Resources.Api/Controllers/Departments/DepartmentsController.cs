using Fusion.AspNetCore;
using Fusion.AspNetCore.FluentAuthorization;
using Fusion.AspNetCore.OData;
using Fusion.Authorization;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands.Departments;
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

        /// <summary>
        /// List all departments with custom auto approval config. 
        /// This is mainly intended for admin utils.
        /// 
        /// Access:
        ///     - Admins
        /// </summary>
        /// <exception cref="ODataException">Unknown value for the mode filter.</exception>
        [HttpGet("/departments/auto-approvals")]
        public async Task<ActionResult<List<ApiDepartmentAutoApproval>>> ListAutoApprovalEntries([FromQuery] ODataQueryParams query)
        {
            #region Authorization
            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen()
                    .FullControl()
                    .FullControlInternal();
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();
            #endregion

            query.MapFilterFields<ApiDepartmentAutoApproval>(m =>
            {
                m.MapToModel<QueryDepartmentAutoApprovalStatus>()
                    .MapField(inModel => inModel.FullDepartmentPath, qModel => qModel.FullDepartmentPath)
                    .MapField(inModel => inModel.Enabled, qModel => qModel.Enabled)
                    .MapField(inModel => inModel.Mode, qModel => qModel.IncludeSubDepartments);                
            });
            
            #region Convert value 
            // We need to convert this value, as the user relates to enum values, which is converted to a bool value internally.
            // If 'in' is used here, it will throw an error, but do not see that as too much of a problem atm.
            foreach (ODataFilterExpression filter in query.Filter.GetFilters())
            {
                if (string.Equals(filter.Field, "IncludeSubDepartments"))
                {
                    filter.Value = filter.Value?.ToLower() switch
                    {
                        "all" => "true",
                        "direct" => "false",
                        _ => throw new ODataException($"Invalid value for field 'mode': '{filter.Value}'")
                    };
                }
            }
            #endregion

            // Must change the value for the mode

            var result = await DispatchAsync(new ListDepartmentAutoApprovals().WithQuery(query));

            return result.Select(x => new ApiDepartmentAutoApproval(x).IncludeFullDepartment()).ToList();
        }



        [HttpGet("/departments/{departmentString}")]
        public async Task<ActionResult<ApiDepartment>> GetDepartments(string departmentString)
        {
            var department = await DispatchAsync(new GetDepartment(departmentString));
            if (department is null) return NotFound();

            var approvalStatus = await DispatchAsync(new GetDepartmentAutoApproval(departmentString));

            return Ok(new ApiDepartment(department, approvalStatus));
        }

        [HttpPatch("/departments/{departmentString}")]
        public async Task<ActionResult<ApiDepartment>> UpdateDepartment(string departmentString, [FromBody] PatchDepartmentRequest request)
        {
            var department = await DispatchAsync(new GetDepartment(departmentString));
            if (department is null) return NotFound();

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    or.BeResourceOwner(new DepartmentPath(departmentString).GoToLevel(1), includeDescendants: true);
                });
            });
            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion


            using (var scope = await BeginTransactionAsync())
            {
                if (request.AutoApproval.HasValue)
                {
                    if (request.AutoApproval.Value is null)
                        await DispatchAsync(SetDepartmentAutoApproval.Remove(departmentString));
                    else
                    {
                        var includeChildren = string.Equals(request.AutoApproval.Value.Mode, $"{ApiDepartmentAutoApprovalMode.All}", StringComparison.OrdinalIgnoreCase);
                        await DispatchAsync(SetDepartmentAutoApproval.Update(departmentString, request.AutoApproval.Value.Enabled, includeChildren));
                    }
                }

                await scope.CommitAsync();
            }


            var approvalStatus = await DispatchAsync(new GetDepartmentAutoApproval(departmentString));
            return Ok(new ApiDepartment(department, approvalStatus));
        }

        [HttpGet("/departments/{departmentString}/related")]
        public async Task<ActionResult<ApiRelatedDepartments>> GetRelevantDepartments(string departmentString)
        {
            var departments = await DispatchAsync(new GetRelatedDepartments(departmentString));
            if (departments is null) return NotFound();

            return Ok(new ApiRelatedDepartments(departments));
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
            if (existingDepartment is null) return NotFound();

            var command = new AddDelegatedResourceOwner(departmentString, request.ResponsibleAzureUniqueId)
            {
                DateFrom = request.DateFrom,
                DateTo = request.DateTo
            };

            await DispatchAsync(command);


            return CreatedAtAction(nameof(GetDepartments), new { departmentString }, null);
        }

        [HttpDelete("/departments/{departmentString}/delegated-resource-owner/{azureUniqueId}")]
        public async Task<IActionResult> DeleteDelegatedResourceOwner(string departmentString, Guid azureUniqueId)
        {
            #region Authorization
            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();
            #endregion

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

            var department = await DispatchAsync(new GetDepartment(routedDepartment));
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
