using System;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Fusion.AspNetCore.FluentAuthorization;
using Fusion.AspNetCore.OData;
using Fusion.Integration;
using Fusion.Resources.Domain.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fusion.Resources.Api.Controllers
{
    [Authorize]
    [ApiController]
    public class InternalRequestsController : ResourceControllerBase
    {
        [HttpPost("/projects/{projectIdentifier}/requests")]
        public async Task<ActionResult<ApiResourceAllocationRequest>> AllocateProjectRequest(
            [FromRoute] ProjectIdentifier projectIdentifier, [FromBody] CreateResourceAllocationRequest request)
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
                .WithAssignedDepartment(request.AssignedDepartment)
                .WithDiscipline(request.Discipline)
                .WithType($"{request.Type}")
                .WithProposedPerson(request.ProposedPersonAzureUniqueId)
                .WithOrgPosition(request.OrgPositionId)
                .WithProposedChanges(request.ProposedChanges)
                .WithIsDraft(request.IsDraft)
                .WithAdditionalNode(request.AdditionalNote);

            if (request.OrgPositionInstance != null)
                command.WithPositionInstance(request.OrgPositionInstance.Id, request.OrgPositionInstance.AppliesFrom,
                              request.OrgPositionInstance.AppliesTo, request.OrgPositionInstance.Workload,
                              request.OrgPositionInstance.Obs, request.OrgPositionInstance.LocationId);


            try
            {
                var result = await DispatchAsync(command);
                return Created($"/projects/{projectIdentifier}/requests/{result.RequestId}",
                    new ApiResourceAllocationRequest(result));
            }
            catch (ProfileNotFoundError pef)
            {
                return ApiErrors.InvalidOperation(pef);
            }
            catch (InvalidOperationException ioe)
            {
                return ApiErrors.InvalidOperation(ioe);
            }
            catch (InvalidOrgChartPositionError ioe)
            {
                return ApiErrors.InvalidOperation(ioe);
            }
            catch (ValidationException ex)
            {
                return ApiErrors.InvalidOperation(ex);
            }
        }

        [HttpPut("/resources/internal-requests/requests/{requestId}")]
        [HttpPut("/projects/{projectIdentifier}/requests/{requestId}")]
        public async Task<ActionResult<ApiResourceAllocationRequest>> UpdateProjectAllocationRequest(
            [FromRoute] ProjectIdentifier? projectIdentifier, Guid requestId,
            [FromBody] UpdateResourceAllocationRequest request)
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

            var command = new Logic.Commands.ResourceAllocationRequest.Update(requestId)
                .WithProjectId(projectIdentifier?.ProjectId)
                .WithAssignedDepartment(request.AssignedDepartment)
                .WithDiscipline(request.Discipline)
                .WithType($"{request.Type}")
                .WithProposedPerson(request.ProposedPersonAzureUniqueId)
                .WithOrgPosition(request.OrgPositionId)
                .WithProposedChanges(request.ProposedChanges)
                .WithIsDraft(request.IsDraft)
                .WithAdditionalNode(request.AdditionalNote);

            if (request.OrgPositionInstance != null)
                command.WithPositionInstance(request.OrgPositionInstance.Id, request.OrgPositionInstance.AppliesFrom,
                              request.OrgPositionInstance.AppliesTo, request.OrgPositionInstance.Workload,
                              request.OrgPositionInstance.Obs, request.OrgPositionInstance.LocationId);


            try
            {
                var result = await DispatchAsync(command);
                return new ApiResourceAllocationRequest(result);
            }
            catch (ProfileNotFoundError pef)
            {
                return ApiErrors.InvalidOperation(pef);
            }
            catch (InvalidOperationException ioe)
            {
                return ApiErrors.InvalidOperation(ioe);
            }
            catch (InvalidOrgChartPositionError ioe)
            {
                return ApiErrors.InvalidOperation(ioe);
            }
            catch (ValidationException ve)
            {
                return ApiErrors.InvalidOperation(ve);
            }
        }


        [HttpGet("/resources/internal-requests/requests")]
        [HttpGet("/projects/{projectIdentifier}/requests")]
        public async Task<ActionResult<ApiCollection<ApiResourceAllocationRequest>>> GetResourceAllocationRequestsForProject(
            [FromRoute] ProjectIdentifier? projectIdentifier, [FromQuery] ODataQueryParams query)
        {
            var requestCommand = new GetResourceAllocationRequests(query);

            if (projectIdentifier != null)
                requestCommand.WithProjectId(projectIdentifier.ProjectId);

            var result = await DispatchAsync(requestCommand);

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

            var apiModel = result.Select(x => new ApiResourceAllocationRequest(x)).ToList();
            return new ApiCollection<ApiResourceAllocationRequest>(apiModel);
        }

        [HttpGet("/resources/internal-requests/requests/{requestId}")]
        [HttpGet("/projects/{projectIdentifier}/requests/{requestId}")]
        public async Task<ActionResult<ApiResourceAllocationRequest>> GetResourceAllocationRequest(Guid requestId)
        {
            var result = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));

            if (result == null)
                return ApiErrors.NotFound("Could not locate request", $"{requestId}");

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

            return new ApiResourceAllocationRequest(result);
        }

        [HttpDelete("/resources/internal-requests/requests/{requestId}")]
        [HttpDelete("/projects/{projectIdentifier}/requests/{requestId}")]
        public async Task<ActionResult> DeleteProjectAllocationRequest(Guid requestId)
        {
            var result = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));

            if (result is null)
                return ApiErrors.NotFound("Could not locate request", $"{requestId}");

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
