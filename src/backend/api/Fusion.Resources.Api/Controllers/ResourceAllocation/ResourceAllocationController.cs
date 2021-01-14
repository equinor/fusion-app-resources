using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Resources.Api.Authorization;
using Fusion.Resources.Domain.Commands;
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

            var authResult = await Request.RequireAuthorizationAsync(r => { r.AlwaysAccessWhen().FullControl(); });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            throw new NotImplementedException();
            var result = new object();
            //await DispatchAsync(new CreateProjectResourceAllocationRequest(request));

            return Created($"/projects/{projectIdentifier}/requests/{request.Id}", new ApiResourceAllocationRequest(null));
        }

        [HttpPatch("/projects/{projectIdentifier}/requests/{requestId}")]
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
        }

        [HttpGet("/projects/{projectIdentifier}/requests")]
        public async Task<ActionResult<List<ApiResourceAllocationRequest>>> GetProjectAllocationRequests(
            [FromRoute] ProjectIdentifier projectIdentifier)
        {
            var result = await DispatchAsync(new GetProjectResourceAllocationRequests(projectIdentifier.ProjectId));

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r => { r.AlwaysAccessWhen().FullControl(); });

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

            var authResult = await Request.RequireAuthorizationAsync(r => { r.AlwaysAccessWhen().FullControl(); });

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
                    or.ProjectAccess(ProjectAccess.ManageContracts, projectIdentifier);
                });

            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            try
            {
                await using var scope = await BeginTransactionAsync();
                await DispatchAsync(new DeleteProjectAllocationRequestCommand(requestId));

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

        [HttpPost("/projects/{projectIdentifier}/requests/{requestId}/approve")]
        public async Task<ActionResult<ApiResourceAllocationRequest>> ApproveProjectAllocationRequest(
            [FromRoute] ProjectIdentifier projectIdentifier, Guid requestId,
            [FromBody] ApproveProjectAllocationRequest request)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r => { r.AlwaysAccessWhen().FullControl(); });

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

            var authResult = await Request.RequireAuthorizationAsync(r => { r.AlwaysAccessWhen().FullControl(); });

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

        [HttpOptions("/projects/{projectIdentifier}/requests/{requestId}")]
        public async Task<ActionResult> CheckProjectAllocationRequestAccess(
            [FromRoute] ProjectIdentifier projectIdentifier, Guid requestId)
        {
            var authResult = await Request.RequireAuthorizationAsync(r => { r.AlwaysAccessWhen().FullControl(); });

            if (authResult.Success)
                Response.Headers.Add("Allow", "GET,PUT,POST,DELETE");
            else
                Response.Headers.Add("Allow", "GET");

            return NoContent();
        }
    }
}