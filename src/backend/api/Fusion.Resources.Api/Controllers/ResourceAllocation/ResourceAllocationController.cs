using System;
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
        public async Task<ActionResult<ApiRequest>> AllocateProjectRequest(
            [FromRoute] ProjectIdentifier projectIdentifier, [FromBody] CreateProjectAllocationRequest request)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r => { r.AlwaysAccessWhen().FullControl(); });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var result = await DispatchAsync(new GetProjectAllocationRequest(request.RequestNumber));

            return Created($"/projects/{projectIdentifier}/requests/{request.RequestNumber}", new ApiRequest(result));
        }

        [HttpPatch("/projects/{projectIdentifier}/requests/{requestId}")]
        public async Task<ActionResult<ApiRequest>> PatchProjectAllocationRequest(
            [FromRoute] ProjectIdentifier projectIdentifier, Guid requestId,
            [FromBody] PatchProjectAllocationRequest request)
        {
            var result = await DispatchAsync(new GetProjectAllocationRequest(requestId));

            if (result == null)
                return ApiErrors.NotFound("Could not locate request", $"{requestId}");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r => { r.AlwaysAccessWhen().FullControl(); });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            await using (var scope = await BeginTransactionAsync())
            {
                result = await DispatchAsync(new ProcessProjectAllocationCommand(requestId, requestId));
                await scope.CommitAsync();
            }

            return new ApiRequest(result);
        }

        [HttpPut("/projects/{projectIdentifier}/requests/{requestId}")]
        public async Task<ActionResult<ApiRequest>> UpdateProjectAllocationRequest(
            [FromRoute] ProjectIdentifier projectIdentifier, Guid requestId,
            [FromBody] CreateProjectAllocationRequest request)
        {
            var result = await DispatchAsync(new GetProjectAllocationRequest(requestId));

            if (result == null)
                return ApiErrors.NotFound("Could not locate request", $"{requestId}");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r => { r.AlwaysAccessWhen().FullControl(); });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            await using (var scope = await BeginTransactionAsync())
            {
                result = await DispatchAsync(new ProcessProjectAllocationCommand(requestId, requestId));

                await scope.CommitAsync();
            }

            return new ApiRequest(result);
        }

        [HttpGet("/projects/{projectIdentifier}/requests/{requestId}")]
        public async Task<ActionResult<ApiRequest>> GetProjectAllocationRequest(
            [FromRoute] ProjectIdentifier projectIdentifier, Guid requestId)
        {
            var result = await DispatchAsync(new GetProjectAllocationRequest(requestId));

            if (result == null)
                return ApiErrors.NotFound("Could not locate request", $"{requestId}");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r => { r.AlwaysAccessWhen().FullControl(); });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            return new ApiRequest(result);
        }

        [HttpDelete("/projects/{projectIdentifier}/requests/{requestId}")]
        public async Task<ActionResult> DeleteProjectAllocationRequest([FromRoute] ProjectIdentifier projectIdentifier,
            Guid requestId)
        {
            var result = await DispatchAsync(new GetProjectAllocationRequest(requestId));

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
                await DispatchAsync(new DeleteProjectAllocationRequest(requestId));

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
        public async Task<ActionResult<ApiRequest>> ApproveProjectAllocationRequest(
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
                await using var scope = await BeginTransactionAsync();
                var result =
                    await DispatchAsync(new ProcessProjectAllocationCommand(projectIdentifier.ProjectId, requestId));

                await scope.CommitAsync();

                return new ApiRequest(result);
            }
            catch (InvalidOperationException ex)
            {
                return ApiErrors.InvalidOperation(ex);
            }
        }

        [HttpPost("/projects/{projectIdentifier}/requests/{requestId}/terminate")]
        public async Task<ActionResult<ApiRequest>> TerminateProjectAllocationRequest(
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
                await using var scope = await BeginTransactionAsync();
                var result =
                    await DispatchAsync(new ProcessProjectAllocationCommand(projectIdentifier.ProjectId, requestId));

                await scope.CommitAsync();

                return new ApiRequest(result);
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