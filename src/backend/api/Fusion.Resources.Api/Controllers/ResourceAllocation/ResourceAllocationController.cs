using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Fusion.AspNetCore.FluentAuthorization;
using Fusion.AspNetCore.OData;
using Fusion.Authorization;
using Fusion.Integration;
using Fusion.Resources.Api.Authorization;
using Fusion.Resources.Domain.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fusion.Resources.Api.Controllers
{
    [Authorize]
    [ApiController]
    public class ResourceAllocationController : ResourceControllerBase
    {
        [HttpPost("/projects/{projectIdentifier}/requests")]
        public async Task<ActionResult<ApiResourceAllocationRequest>> AllocateProjectRequest(
            [FromRoute] ProjectIdentifier projectIdentifier, [FromBody] CreateProjectAllocationRequest request)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();
                r.AnyOf(or =>
                {

                });
            });


            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var command = new Logic.Commands.ResourceAllocationRequest.Create(projectIdentifier.ProjectId)
                .WithDiscipline(request.Discipline)
                .WithType($"{request.Type}")
                .WithProposedPerson(request.ProposedPersonId)
                .WithOrgPosition(request.OrgPositionId)
                .WithProposedChanges(request.ProposedChanges)
                .WithIsDraft(request.IsDraft)
                .WithAdditionalNode(request.AdditionalNote)
                .WithPosition(request.OrgPositionInstance.Id, request.OrgPositionInstance.AppliesFrom,
                              request.OrgPositionInstance.AppliesTo, request.OrgPositionInstance.Workload,
                              request.OrgPositionInstance.Obs, request.OrgPositionInstance.Location);


            try
            {
                var result = await DispatchAsync(command);
                return Created($"/projects/{projectIdentifier}/requests/{result.RequestId}",
                    new ApiResourceAllocationRequest(result));
            }
            catch (ProfileNotFoundError pef)
            {
                return FusionApiError.InvalidOperation("ProfileNotFound", pef.Message);
            }
            catch (InvalidOperationException ioe)
            {
                return FusionApiError.InvalidOperation("InvalidOperation", ioe.Message);
            }
            catch (InvalidOrgChartPositionError ioe)
            {
                return FusionApiError.InvalidOperation("InvalidOperation", ioe.Message);
            }
        }

        /*[HttpPatch("/projects/{projectIdentifier}/requests/{requestId}")]
        public async Task<ActionResult<ApiResourceAllocationRequest>> PatchProjectAllocationRequest(
            [FromRoute] ProjectIdentifier projectIdentifier, Guid requestId,
            [FromBody] PatchProjectAllocationRequest request)
        {
            var result = await DispatchAsync(new GetProjectResourceAllocationRequestItem(requestId));

            if (result == null)
                return ApiErrors.NotFound("Could not locate request", $"{requestId}");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r => { r.AlwaysAccessWhen().FullControl(); });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            await using (var scope = await BeginTransactionAsync())
            {
                throw new NotImplementedException();
                //result = await DispatchAsync(new Domain.Commands.PatchProjectAllocationRequestCommand(requestId, request));
                await scope.CommitAsync();
            }

            return new ApiResourceAllocationRequest(result);
        }

        [HttpPut("/projects/{projectIdentifier}/requests/{requestId}")]
        public async Task<ActionResult<ApiResourceAllocationRequest>> UpdateProjectAllocationRequest(
            [FromRoute] ProjectIdentifier projectIdentifier, Guid requestId,
            [FromBody] CreateProjectAllocationRequest request)
        {
            var result = await DispatchAsync(new GetProjectResourceAllocationRequestItem(requestId));

            if (result == null)
                return ApiErrors.NotFound("Could not locate request", $"{requestId}");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r => { r.AlwaysAccessWhen().FullControl(); });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            await using (var scope = await BeginTransactionAsync())
            {
                throw new NotImplementedException();
                //result = await DispatchAsync(new Domain.Commands.PutProjectAllocationRequestCommand(requestId, request));
                await scope.CommitAsync();
            }

            return new ApiResourceAllocationRequest(result);
        }*/

        [HttpGet("/projects/{projectIdentifier}/requests")]
        public async Task<ActionResult<List<ApiResourceAllocationRequest>>> GetProjectAllocationRequests(
            [FromRoute] ProjectIdentifier projectIdentifier, [FromQuery] ODataQueryParams query)
        {
            var result = await DispatchAsync(new GetProjectResourceAllocationRequests(projectIdentifier.ProjectId, query));

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();
                r.AnyOf(or =>
                {
                    or.BeEmployee();

                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            return result.Select(x => new ApiResourceAllocationRequest(x)).ToList();
        }
        [HttpGet("/projects/{projectIdentifier}/requests/{requestId}")]
        public async Task<ActionResult<ApiResourceAllocationRequest>> GetProjectAllocationRequest(
            [FromRoute] ProjectIdentifier projectIdentifier, Guid requestId)
        {
            var result = await DispatchAsync(new GetProjectResourceAllocationRequestItem(requestId));

            if (result == null)
                return ApiErrors.NotFound("Could not locate request", $"{requestId}");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();
                r.AnyOf(or =>
                {
                    or.BeEmployee();

                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            return new ApiResourceAllocationRequest(result);
        }

        [HttpDelete("/projects/{projectIdentifier}/requests/{requestId}")]
        public async Task<ActionResult> DeleteProjectAllocationRequest([FromRoute] ProjectIdentifier projectIdentifier,
            Guid requestId)
        {
            var result = await DispatchAsync(new GetProjectResourceAllocationRequestItem(requestId));

            if (result == null)
                return ApiErrors.NotFound("Could not locate request", $"{requestId}");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();
                r.AnyOf(or =>
                {
                    or.ProjectAccess(ProjectAccess.ManageRequests, projectIdentifier);
                });

            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            try
            {
                await using var scope = await BeginTransactionAsync();
                await DispatchAsync(new Logic.Commands.ResourceAllocationRequest.Delete(requestId));

                await scope.CommitAsync();
            }
            catch (InvalidOperationException ex)
            {
                return ApiErrors.InvalidOperation(ex);
            }
            catch (ValidationException ex)
            {
                return ApiErrors.InvalidOperation(ex);
            }

            return Ok();
        }
        /*
        [HttpPost("/projects/{projectIdentifier}/requests/{requestId}/approve")]
        public async Task<ActionResult<ApiResourceAllocationRequest>> ApproveProjectAllocationRequest(
            [FromRoute] ProjectIdentifier projectIdentifier, Guid requestId,
            [FromBody] ApproveProjectAllocationRequest request)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();
                r.AnyOf(or =>
                {
                    
                    or.ProjectAccess(ProjectAccess.ManageRequests, projectIdentifier);
                });

            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion


            try
            {
                throw new NotImplementedException();
                await using var scope = await BeginTransactionAsync();
                var result = new object();
                //await DispatchAsync(new CreateProjectAllocationRequestCommand(projectIdentifier.ProjectId, requestId));

                await scope.CommitAsync();

                return new ApiResourceAllocationRequest(null);
            }
            catch (InvalidOperationException ex)
            {
                return ApiErrors.InvalidOperation(ex);
            }
        }

        [HttpPost("/projects/{projectIdentifier}/requests/{requestId}/terminate")]
        public async Task<ActionResult<ApiResourceAllocationRequest>> TerminateProjectAllocationRequest(
            [FromRoute] ProjectIdentifier projectIdentifier, Guid requestId,
            [FromBody] TerminateProjectAllocationRequest request)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();
                r.AnyOf(or =>
                {
                    
                    or.ProjectAccess(ProjectAccess.ManageRequests, projectIdentifier);
                });

            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            try
            {
                throw new NotImplementedException();
                await using var scope = await BeginTransactionAsync();
                var result = new object();
                //await DispatchAsync(new ProcessProjectAllocationCommand(projectIdentifier.ProjectId, requestId));

                await scope.CommitAsync();

                return new ApiResourceAllocationRequest(null);
            }
            catch (InvalidOperationException ex)
            {
                return ApiErrors.InvalidOperation(ex);
            }
        }
        */
        [HttpOptions("/projects/{projectIdentifier}/requests/{requestId}")]
        public async Task<ActionResult> CheckProjectAllocationRequestAccess(
            [FromRoute] ProjectIdentifier projectIdentifier, Guid requestId)
        {
            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();
                r.AnyOf(or =>
                {
                    or.ProjectAccess(ProjectAccess.ManageRequests, projectIdentifier);
                });

            });

            if (authResult.Success)
                Response.Headers.Add("Allow", "GET,PUT,POST,DELETE");
            else
                Response.Headers.Add("Allow", "GET");

            return NoContent();
        }
    }
}