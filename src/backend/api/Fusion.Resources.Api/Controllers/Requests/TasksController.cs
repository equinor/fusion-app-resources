using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Authorization;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands;
using Fusion.Resources.Domain.Commands.Tasks;
using Fusion.Resources.Domain.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Controllers.Requests
{
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    public class TasksController : ResourceControllerBase
    {
        [HttpPost("requests/{requestId}/tasks")]
        public async Task<ActionResult> AddRequestTask([FromRoute] Guid requestId, [FromBody] AddRequestTaskRequest request)
        {
            var requestItem = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));
            if (requestItem is null) return FusionApiError.NotFound(requestId, $"Request with id '{requestId}' was not found.");

            #region Authorization
            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    if (requestItem.OrgPositionId.HasValue)
                        or.OrgChartPositionWriteAccess(requestItem.Project.OrgProjectId, requestItem.OrgPositionId.Value);

                    if (requestItem.AssignedDepartment is not null)
                    {
                        or.BeResourceOwner(
                            new DepartmentPath(requestItem.AssignedDepartment).GoToLevel(2),
                            includeParents: false,
                            includeDescendants: true
                        );
                    }
                    else
                    {
                        or.BeResourceOwner();
                    }

                    or.BeRequestCreator(requestId);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();
            #endregion

            var command = new AddRequestTask(requestId, request.Title, request.Body, request.Category, request.Type)
            {
                SubType = request.SubType,
                Source = request.Source switch
                {
                    ApiTaskSource.ResourceOwner => TaskSource.ResourceOwner,
                    ApiTaskSource.TaskOwner => TaskSource.TaskOwner,
                    _ => throw new NotSupportedException($"Could not map {request.Source} to {nameof(TaskSource)}.")
                },
                Responsible = request.Responsible switch
                {
                    ApiTaskResponsible.ResourceOwner => TaskResponsible.ResourceOwner,
                    ApiTaskResponsible.TaskOwner => TaskResponsible.TaskOwner,
                    ApiTaskResponsible.Both => TaskResponsible.Both,
                    _ => throw new NotSupportedException($"Could not map {request.Source} to {nameof(TaskSource)}.")
                },
                Properties = request.Properties
            };

            var created = await DispatchAsync(command);

            return CreatedAtAction(nameof(GetRequestTask), new { requestId, taskId = created.Id }, new ApiRequestTask(created));
        }

        [HttpGet("requests/{requestId}/tasks")]
        public async Task<ActionResult> GetRequestTasks([FromRoute] Guid requestId)
        {
            var request = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));
            if (request is null) return FusionApiError.NotFound(requestId, $"Request with id '{requestId}' was not found.");

            #region Authorization
            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    if (request.OrgPositionId.HasValue)
                        or.OrgChartPositionWriteAccess(request.Project.OrgProjectId, request.OrgPositionId.Value);

                    if (request.AssignedDepartment is not null)
                    {
                        or.BeResourceOwner(
                            new DepartmentPath(request.AssignedDepartment).GoToLevel(2),
                            includeParents: false,
                            includeDescendants: true
                        );
                    }
                    else
                    {
                        or.BeResourceOwner();
                    }

                    or.BeRequestCreator(requestId);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();
            #endregion

            var command = new GetRequestTasks(requestId);
            var tasks = await DispatchAsync(command);

            return Ok(tasks.Select(t => new ApiRequestTask(t)));
        }

        [HttpGet("requests/{requestId}/tasks/{taskId}")]
        public async Task<ActionResult> GetRequestTask([FromRoute] Guid requestId, [FromRoute] Guid taskId)
        {
            var request = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));
            if (request is null) return FusionApiError.NotFound(requestId, $"Request with id '{requestId}' was not found.");

            #region Authorization
            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    if (request.OrgPositionId.HasValue)
                        or.OrgChartPositionWriteAccess(request.Project.OrgProjectId, request.OrgPositionId.Value);

                    if (request.AssignedDepartment is not null)
                    {
                        or.BeResourceOwner(
                            new DepartmentPath(request.AssignedDepartment).GoToLevel(2),
                            includeParents: false,
                            includeDescendants: true
                        );
                    }
                    else
                    {
                        or.BeResourceOwner();
                    }

                    or.BeRequestCreator(requestId);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();
            #endregion

            var command = new GetRequestTask(requestId, taskId);
            var task = await DispatchAsync(command);
            if (task is null) return FusionApiError.NotFound(taskId, $"A task with id '{taskId}' was not found on request with id '{requestId}'.");

            return Ok(new ApiRequestTask(task));
        }

        [HttpPatch("requests/{requestId}/tasks/{taskId}")]
        public async Task<ActionResult> UpdateRequestTask([FromRoute] Guid requestId, [FromRoute] Guid taskId, [FromBody] PatchRequestTaskRequest patch)
        {
            var request = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));
            if (request is null) return FusionApiError.NotFound(requestId, $"Request with id '{requestId}' was not found.");

            #region Authorization
            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    if (request.OrgPositionId.HasValue)
                        or.OrgChartPositionWriteAccess(request.Project.OrgProjectId, request.OrgPositionId.Value);

                    if (request.AssignedDepartment is not null)
                    {
                        or.BeResourceOwner(
                            new DepartmentPath(request.AssignedDepartment).GoToLevel(2),
                            includeParents: false,
                            includeDescendants: true
                        );
                    }
                    else
                    {
                        or.BeResourceOwner();
                    }

                    or.BeRequestCreator(requestId);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();
            #endregion

            var command = new UpdateRequestTask(requestId, taskId);

            if (patch.Title.HasValue) command.Title = patch.Title.Value;
            if (patch.Body.HasValue) command.Body = patch.Body.Value;
            if (patch.Category.HasValue) command.Category = patch.Category.Value;
            if (patch.Type.HasValue) command.Type = patch.Type.Value;
            if (patch.SubType.HasValue) command.SubType = patch.SubType.Value;
            if (patch.IsResolved.HasValue) command.IsResolved = patch.IsResolved.Value;
            if (patch.Properties.HasValue) command.Properties = patch.Properties.Value;

            try
            {
                var updated = await DispatchAsync(command);
                return Ok(new ApiRequestTask(updated));
            }
            catch (TaskNotFoundError err)
            {
                return FusionApiError.NotFound(taskId, err.Message);
            }
        }

        [HttpDelete("requests/{requestId}/tasks/{taskId}")]
        public async Task<ActionResult> DeleteRequestTask([FromRoute] Guid requestId, [FromRoute] Guid taskId)
        {
            var request = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));
            if (request is null) return FusionApiError.NotFound(requestId, $"Request with id '{requestId}' was not found.");

            #region Authorization
            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    if (request.OrgPositionId.HasValue)
                        or.OrgChartPositionWriteAccess(request.Project.OrgProjectId, request.OrgPositionId.Value);

                    if (request.AssignedDepartment is not null)
                    {
                        or.BeResourceOwner(
                            new DepartmentPath(request.AssignedDepartment).GoToLevel(2),
                            includeParents: false,
                            includeDescendants: true
                        );
                    }
                    else
                    {
                        or.BeResourceOwner();
                    }

                    or.BeRequestCreator(requestId);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();
            #endregion

            var command = new DeleteRequestTask(requestId, taskId);
            var wasDeleted = await DispatchAsync(command);

            if (wasDeleted) return NoContent();
            else return FusionApiError.NotFound(taskId, $"A task with id '{taskId}' was not found on request with id '{requestId}'.");
        }
    }
}
