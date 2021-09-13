using Fusion.AspNetCore.FluentAuthorization;
using Fusion.AspNetCore.OData;
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
    public class RequestActionsController : ResourceControllerBase
    {
        [HttpPost("requests/{requestId}/actions")]
        public async Task<ActionResult> AddRequestActionAsync([FromRoute] Guid requestId, [FromBody] AddActionRequest request)
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

            var command = new AddRequestAction(requestId, request.Title, request.Body, request.Type)
            {
                SubType = request.SubType,
                Source = request.Source switch
                {
                    ApiTaskSource.ResourceOwner => QueryTaskSource.ResourceOwner,
                    ApiTaskSource.TaskOwner => QueryTaskSource.TaskOwner,
                    _ => throw new NotSupportedException($"Could not map {request.Source} to {nameof(QueryTaskSource)}.")
                },
                Responsible = request.Responsible switch
                {
                    ApiTaskResponsible.ResourceOwner => QueryTaskResponsible.ResourceOwner,
                    ApiTaskResponsible.TaskOwner => QueryTaskResponsible.TaskOwner,
                    ApiTaskResponsible.Both => QueryTaskResponsible.Both,
                    _ => throw new NotSupportedException($"Could not map {request.Source} to {nameof(QueryTaskSource)}.")
                },
                IsRequired = request.IsRequired,
                Properties = request.Properties,
                DueDate = request.DueDate,
                AssignedToId = request.AssignedToId
            };

            var created = await DispatchAsync(command);

            return CreatedAtAction(nameof(GetRequestAction), new { requestId, actionId = created.Id }, new ApiRequestAction(created));
        }

        [HttpGet("requests/{requestId}/actions")]
        public async Task<ActionResult> GetRequestActions([FromRoute] Guid requestId, [FromRoute] ODataQueryParams query)
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

            var command = new GetRequestActions(requestId).WithQuery(query);
            var tasks = await DispatchAsync(command);

            return Ok(tasks.Select(t => new ApiRequestAction(t)));
        }

        [HttpGet("requests/{requestId}/actions/{actionId}")]
        public async Task<ActionResult> GetRequestAction([FromRoute] Guid requestId, [FromRoute] Guid actionId)
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

            var command = new GetRequestAction(requestId, actionId);
            var task = await DispatchAsync(command);
            if (task is null) return FusionApiError.NotFound(actionId, $"A task with id '{actionId}' was not found on request with id '{requestId}'.");

            return Ok(new ApiRequestAction(task));
        }

        [HttpPatch("requests/{requestId}/actions/{actionId}")]
        public async Task<ActionResult> UpdateRequestAction([FromRoute] Guid requestId, [FromRoute] Guid actionId, [FromBody] PatchActionRequest patch)
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

            var command = new UpdateRequestAction(requestId, actionId);

            if (patch.Title.HasValue) command.Title = patch.Title.Value;
            if (patch.Body.HasValue) command.Body = patch.Body.Value;
            if (patch.Type.HasValue) command.Type = patch.Type.Value;
            if (patch.SubType.HasValue) command.SubType = patch.SubType.Value;
            if (patch.IsResolved.HasValue) command.IsResolved = patch.IsResolved.Value;
            if (patch.IsRequired.HasValue) command.IsRequired = patch.IsRequired.Value;
            if (patch.Properties.HasValue) command.Properties = patch.Properties.Value;
            if (patch.AssignedToId.HasValue) command.AssignedToId = patch.AssignedToId.Value;
            if (patch.DueDate.HasValue) command.DueDate = patch.DueDate.Value;

            try
            {
                var updated = await DispatchAsync(command);
                return Ok(new ApiRequestAction(updated));
            }
            catch (TaskNotFoundError err)
            {
                return FusionApiError.NotFound(actionId, err.Message);
            }
        }

        [HttpDelete("requests/{requestId}/actions/{actionId}")]
        public async Task<ActionResult> DeleteRequestAction([FromRoute] Guid requestId, [FromRoute] Guid actionId)
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

            var command = new DeleteRequestAction(requestId, actionId);
            var wasDeleted = await DispatchAsync(command);

            if (wasDeleted) return NoContent();
            else return FusionApiError.NotFound(actionId, $"A task with id '{actionId}' was not found on request with id '{requestId}'.");
        }
    }
}
