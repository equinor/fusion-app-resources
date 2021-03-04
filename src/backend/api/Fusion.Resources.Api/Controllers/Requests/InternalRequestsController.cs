using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Fusion.AspNetCore.FluentAuthorization;
using Fusion.AspNetCore.OData;
using Fusion.Integration;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands;
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
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {

                });
            });


            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion
            try
            {
                QueryResourceAllocationRequest? result = null;
                switch (request.Type)
                {
                    case ApiAllocationRequestType.Normal:
                        var command = new Logic.Commands.ResourceAllocationRequest.Normal.Create(projectIdentifier.ProjectId)
                            .WithAssignedDepartment(request.AssignedDepartment)
                            .WithDiscipline(request.Discipline)
                            .WithType($"{request.Type}")
                            .WithProposedPerson(request.ProposedPersonAzureUniqueId)
                            .WithOrgPosition(request.OrgPositionId)
                            .WithProposedChanges(request.ProposedChanges)
                            .WithIsDraft(request.IsDraft)
                            .WithAdditionalNode(request.AdditionalNote);

                        if (request.OrgPositionInstance != null)
                            command.WithPositionInstance(request.OrgPositionInstance.Id,
                                request.OrgPositionInstance.AppliesFrom,
                                request.OrgPositionInstance.AppliesTo, request.OrgPositionInstance.Workload,
                                request.OrgPositionInstance.Obs, request.OrgPositionInstance.LocationId);


                        result = await DispatchAsync(command);
                        break;
                    case ApiAllocationRequestType.Direct:
                        var direct = new Logic.Commands.ResourceAllocationRequest.Direct.Create(projectIdentifier.ProjectId)
                            .WithAssignedDepartment(request.AssignedDepartment)
                            .WithDiscipline(request.Discipline)
                            .WithType($"{request.Type}")
                            .WithProposedPerson(request.ProposedPersonAzureUniqueId)
                            .WithOrgPosition(request.OrgPositionId)
                            .WithProposedChanges(request.ProposedChanges)
                            .WithIsDraft(request.IsDraft)
                            .WithAdditionalNode(request.AdditionalNote);

                        if (request.OrgPositionInstance != null)
                            direct.WithPositionInstance(request.OrgPositionInstance.Id,
                                request.OrgPositionInstance.AppliesFrom,
                                request.OrgPositionInstance.AppliesTo, request.OrgPositionInstance.Workload,
                                request.OrgPositionInstance.Obs, request.OrgPositionInstance.LocationId);


                        result = await DispatchAsync(direct);
                        break;
                    case ApiAllocationRequestType.JointVenture:
                        var jointVenture = new Logic.Commands.ResourceAllocationRequest.JointVenture.Create(projectIdentifier.ProjectId)
                            .WithAssignedDepartment(request.AssignedDepartment)
                            .WithDiscipline(request.Discipline)
                            .WithType($"{request.Type}")
                            .WithProposedPerson(request.ProposedPersonAzureUniqueId)
                            .WithOrgPosition(request.OrgPositionId)
                            .WithProposedChanges(request.ProposedChanges)
                            .WithIsDraft(request.IsDraft)
                            .WithAdditionalNode(request.AdditionalNote);

                        if (request.OrgPositionInstance != null)
                            jointVenture.WithPositionInstance(request.OrgPositionInstance.Id,
                                request.OrgPositionInstance.AppliesFrom,
                                request.OrgPositionInstance.AppliesTo, request.OrgPositionInstance.Workload,
                                request.OrgPositionInstance.Obs, request.OrgPositionInstance.LocationId);


                        result = await DispatchAsync(jointVenture);
                        break;

                }

                return Created($"/projects/{projectIdentifier}/requests/{result!.RequestId}", new ApiResourceAllocationRequest(result));
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

        [HttpPut("/resources/requests/internal/{requestId}")]
        [HttpPut("/projects/{projectIdentifier}/requests/{requestId}")]
        public async Task<ActionResult<ApiResourceAllocationRequest>> UpdateProjectAllocationRequest(
            [FromRoute] ProjectIdentifier? projectIdentifier, Guid requestId,
            [FromBody] UpdateResourceAllocationRequest request)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {

                });
            });


            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion
            try
            {
                var item = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));

                if (item == null)
                    return ApiErrors.NotFound("Could not locate request", $"{requestId}");
                QueryResourceAllocationRequest? result = null;
                switch (item.Type)
                {
                    case QueryResourceAllocationRequest.QueryAllocationRequestType.Normal:
                        var normal = new Logic.Commands.ResourceAllocationRequest.Normal.Update(requestId)
                            .WithAssignedDepartment(request.AssignedDepartment)
                            .WithDiscipline(request.Discipline)
                            .WithProposedPerson(request.ProposedPersonAzureUniqueId)
                            .WithProposedChanges(request.ProposedChanges)
                            .WithIsDraft(request.IsDraft)
                            .WithAdditionalNode(request.AdditionalNote);

                        if (request.OrgPositionInstance != null)
                            normal.WithPositionInstance(request.OrgPositionInstance.Id, request.OrgPositionInstance.AppliesFrom,
                                request.OrgPositionInstance.AppliesTo, request.OrgPositionInstance.Workload,
                                request.OrgPositionInstance.Obs, request.OrgPositionInstance.LocationId);

                        result = await DispatchAsync(normal);
                        break;
                    case QueryResourceAllocationRequest.QueryAllocationRequestType.Direct:
                        var direct = new Logic.Commands.ResourceAllocationRequest.Direct.Update(requestId)
                            .WithAssignedDepartment(request.AssignedDepartment)
                            .WithDiscipline(request.Discipline)
                            .WithProposedPerson(request.ProposedPersonAzureUniqueId)
                            .WithProposedChanges(request.ProposedChanges)
                            .WithIsDraft(request.IsDraft)
                            .WithAdditionalNode(request.AdditionalNote);

                        if (request.OrgPositionInstance != null)
                            direct.WithPositionInstance(request.OrgPositionInstance.Id, request.OrgPositionInstance.AppliesFrom,
                                request.OrgPositionInstance.AppliesTo, request.OrgPositionInstance.Workload,
                                request.OrgPositionInstance.Obs, request.OrgPositionInstance.LocationId);

                        result = await DispatchAsync(direct);
                        break;
                    case QueryResourceAllocationRequest.QueryAllocationRequestType.JointVenture:
                        var jointVenture = new Logic.Commands.ResourceAllocationRequest.JointVenture.Update(requestId)
                            .WithAssignedDepartment(request.AssignedDepartment)
                            .WithDiscipline(request.Discipline)
                            .WithProposedPerson(request.ProposedPersonAzureUniqueId)
                            .WithProposedChanges(request.ProposedChanges)
                            .WithIsDraft(request.IsDraft)
                            .WithAdditionalNode(request.AdditionalNote);

                        if (request.OrgPositionInstance != null)
                            jointVenture.WithPositionInstance(request.OrgPositionInstance.Id, request.OrgPositionInstance.AppliesFrom,
                                request.OrgPositionInstance.AppliesTo, request.OrgPositionInstance.Workload,
                                request.OrgPositionInstance.Obs, request.OrgPositionInstance.LocationId);
                        result = await DispatchAsync(jointVenture);
                        break;
                }
                return new ApiResourceAllocationRequest(result!);
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

        [HttpPatch("/resources/requests/internal/{requestId}")]
        [HttpPatch("/projects/{projectIdentifier}/requests/{requestId}")]
        public async Task<ActionResult<ApiResourceAllocationRequest>> PatchInternalRequest([FromRoute] ProjectIdentifier? projectIdentifier, Guid requestId, [FromBody] PatchInternalRequestRequest request)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {

                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            try
            {
                var item = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));

                if (item == null)
                    return ApiErrors.NotFound("Could not locate request", $"{requestId}");


                var updateCommand = new UpdateInternalRequest(requestId);

                if (request.AdditionalNote.HasValue) updateCommand.AdditionalNote = request.AdditionalNote.Value;
                if (request.AssignedDepartment.HasValue) updateCommand.AssignedDepartment = request.AssignedDepartment.Value;
                if (request.IsDraft.HasValue) updateCommand.IsDraft = request.IsDraft.Value;
                if (request.ProposedChanges.HasValue) updateCommand.ProposedChanges = request.ProposedChanges.Value;
                if (request.ProposedPersonAzureUniqueId.HasValue) updateCommand.ProposedPersonAzureUniqueId = request.ProposedPersonAzureUniqueId.Value;

                await using var scope = await BeginTransactionAsync();
                var updatedRequest = await DispatchAsync(updateCommand);
                await scope.CommitAsync();

                return new ApiResourceAllocationRequest(updatedRequest);
            }
            catch (ValidationException ve)
            {
                return ApiErrors.InvalidOperation(ve);
            }
        }


        [HttpGet("/resources/requests/internal")]
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
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
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

        [HttpGet("/resources/requests/internal/unassigned")]
        public async Task<ActionResult<ApiCollection<ApiResourceAllocationRequest>>> GetUnassignedRequests([FromQuery] ODataQueryParams query)
        {
            var countEnabled = Request.Query.ContainsKey("$count");

            var requestCommand = new GetResourceAllocationRequests(query)
                .WithUnassignedFilter(true)
                .WithOnlyCount(countEnabled)
                .WithExcludeDrafts();

            var result = await DispatchAsync(requestCommand);

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {

                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var apiModel = result.Select(x => new ApiResourceAllocationRequest(x)).ToList();

            var start = result.Skip;
            var end = start + result.Count;
            Request.Headers.Add("Accept-Ranges", "requests");
            Request.Headers.Add("Content-Range", $"requests {start}-{end}/{result.TotalCount}");

            return new ApiCollection<ApiResourceAllocationRequest>(apiModel)
            {
                TotalCount = countEnabled ? result.TotalCount : null
            };
        }

        [HttpGet("/resources/requests/internal/{requestId}")]
        [HttpGet("/projects/{projectIdentifier}/requests/{requestId}")]
        public async Task<ActionResult<ApiResourceAllocationRequest>> GetResourceAllocationRequest(Guid requestId, [FromQuery] ODataQueryParams query)
        {
            var result = await DispatchAsync(new GetResourceAllocationRequestItem(requestId).WithQuery(query));

            if (result == null)
                return ApiErrors.NotFound("Could not locate request", $"{requestId}");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {

                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            return new ApiResourceAllocationRequest(result);
        }

        [HttpDelete("/resources/requests/internal/{requestId}")]
        [HttpDelete("/projects/{projectIdentifier}/requests/{requestId}")]
        public async Task<ActionResult> DeleteProjectAllocationRequest(Guid requestId)
        {
            var result = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));

            if (result is null)
                return ApiErrors.NotFound("Could not locate request", $"{requestId}");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
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

        [HttpPost("/resources/requests/internal/{requestId}/provision")]
        public async Task<ActionResult<ApiResourceAllocationRequest>> ProvisionProjectAllocationRequest(Guid requestId)
        {
            var result = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));

            if (result == null)
                return ApiErrors.NotFound("Could not locate request", $"{requestId}");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
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

                switch (result.Type)
                {
                    case QueryResourceAllocationRequest.QueryAllocationRequestType.Direct:
                        await DispatchAsync(new Logic.Commands.ResourceAllocationRequest.Direct.Provision(requestId));
                        break;
                }

                await scope.CommitAsync();

                result = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));
                return new ApiResourceAllocationRequest(result!);
            }
            catch (ValidationException ex)
            {
                return ApiErrors.InvalidOperation(ex);
            }
        }

        [HttpPost("/projects/{projectIdentifier}/requests/{requestId}/approve")]
        public async Task<ActionResult<ApiResourceAllocationRequest>> ApproveProjectAllocationRequest(
            [FromRoute] ProjectIdentifier projectIdentifier, Guid requestId)
        {
            var result = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));

            if (result == null)
                return ApiErrors.NotFound("Could not locate request", $"{requestId}");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
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

                switch (result.Type)
                {
                    case QueryResourceAllocationRequest.QueryAllocationRequestType.Normal:
                        await DispatchAsync(new Logic.Commands.ResourceAllocationRequest.Normal.Approve(requestId));
                        break;
                    case QueryResourceAllocationRequest.QueryAllocationRequestType.Direct:
                        await DispatchAsync(new Logic.Commands.ResourceAllocationRequest.Direct.Approve(requestId));
                        break;
                    case QueryResourceAllocationRequest.QueryAllocationRequestType.JointVenture:
                        await DispatchAsync(new Logic.Commands.ResourceAllocationRequest.JointVenture.Approve(requestId));
                        break;
                }

                await scope.CommitAsync();

                result = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));
                return new ApiResourceAllocationRequest(result!);
            }
            catch (NotSupportedException ex)
            {
                return ApiErrors.InvalidOperation(ex);
            }
            catch (InvalidOperationException ex)
            {
                return ApiErrors.InvalidOperation(ex);
            }
        }

        #region Comments
        [HttpPost("/resources/requests/internal/{requestId}/comments")]
        public async Task<ActionResult<ApiRequestComment>> AddRequestComment(Guid requestId, [FromBody] RequestCommentRequest create)
        {
            var request = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));

            if (request == null)
                return FusionApiError.NotFound(requestId, "Request not found");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion
            
            var comment = await DispatchAsync(new AddComment(User.GetRequestOrigin(), requestId, create.Content));

            return Created($"/resources/requests/internal/{requestId}/comments/{comment.Id}", new ApiRequestComment(comment));
        }

        [HttpGet("/resources/requests/internal/{requestId}/comments")]
        public async Task<ActionResult<IEnumerable<ApiRequestComment>>> GetRequestComment(Guid requestId)
        {
            var request = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));

            if (request == null)
                return FusionApiError.NotFound(requestId, "Request not found");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var comments = await DispatchAsync(new GetRequestComments(requestId));
            return comments.Select(x => new ApiRequestComment(x)).ToList();
        }
        [HttpGet("/resources/requests/internal/{requestId}/comments/{commentId}")]
        public async Task<ActionResult<ApiRequestComment>> GetRequestComment(Guid requestId, Guid commentId)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var comment = await DispatchAsync(new GetRequestComment(commentId));
            return new ApiRequestComment(comment!);
        }

        [HttpPut("/resources/requests/internal/{requestId}/comments/{commentId}")]
        public async Task<ActionResult<ApiRequestComment>> UpdateRequestComment(Guid requestId, Guid commentId, [FromBody] RequestCommentRequest update)
        {
            var comment = await DispatchAsync(new GetRequestComment(commentId));

            if (comment is null)
                return FusionApiError.NotFound(commentId, "Comment not found");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            await DispatchAsync(new UpdateComment(commentId, update.Content));

            comment = await DispatchAsync(new GetRequestComment(commentId));
            return new ApiRequestComment(comment!);
        }

        [HttpDelete("/resources/requests/internal/{requestId}/comments/{commentId}")]
        public async Task<ActionResult> DeleteRequestComment(Guid requestId, Guid commentId)
        {
            var comment = await DispatchAsync(new GetRequestComment(commentId));

            if (comment is null)
                return FusionApiError.NotFound(commentId, "Comment not found");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();

            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            await DispatchAsync(new DeleteComment(commentId));

            return NoContent();
        }

        #endregion Comments

        [HttpOptions("/projects/{projectIdentifier}/requests/{requestId}/approve")]
        public async Task<ActionResult<ApiResourceAllocationRequest>> CheckApprovalAccess([FromRoute] ProjectIdentifier projectIdentifier, Guid requestId)
        {
            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                });

            });


            if (authResult.Success)
                Response.Headers.Add("Allow", "GET,POST");
            else
                Response.Headers.Add("Allow", "GET");

            return NoContent();
        }
        [HttpOptions("/projects/{projectIdentifier}/requests/{requestId}")]
        public async Task<ActionResult> CheckProjectAllocationRequestAccess([FromRoute] ProjectIdentifier projectIdentifier, Guid requestId)
        {
            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
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
