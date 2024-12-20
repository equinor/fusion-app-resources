﻿using Fusion.AspNetCore.FluentAuthorization;
using Fusion.AspNetCore.OData;
using Fusion.Authorization;
using Fusion.Integration.LineOrg;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands;
using Fusion.Resources.Domain.Commands.Tasks;
using Fusion.Resources.Domain.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
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

            if (requestItem.IsCompleted)
                return FusionApiError.InvalidOperation("ActionsDisabled", "Cannot add action on closed request");

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
                        or.BeResourceOwnerForDepartment(
                            new DepartmentPath(requestItem.AssignedDepartment).GoToLevel(2),
                            includeParents: false,
                            includeDescendants: true
                        );
                        or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(requestItem.AssignedDepartment), AccessRoles.ResourceOwner);
                    }
                    else
                    {
                        or.BeResourceOwnerForAnyDepartment();
                        or.HaveAnyOrgUnitScopedRole(AccessRoles.ResourceOwner);
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
        public async Task<ActionResult> GetRequestActions([FromRoute] Guid requestId, [FromQuery] ODataQueryParams query)
        {
            var request = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));
            if (request is null) return FusionApiError.NotFound(requestId, $"Request with id '{requestId}' was not found.");

            #region Authorization
            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.LimitedAccessWhen(or =>
                {
                    if (request.OrgPositionId.HasValue)
                        or.OrgChartPositionWriteAccess(request.Project.OrgProjectId, request.OrgPositionId.Value);
                });
                r.AnyOf(or =>
                {
                    if (request.AssignedDepartment is not null)
                    {
                        or.BeResourceOwnerForDepartment(
                            new DepartmentPath(request.AssignedDepartment).GoToLevel(2),
                            includeParents: false,
                            includeDescendants: true
                        );
                        or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(request.AssignedDepartment), AccessRoles.ResourceOwner);
                    }
                    else
                    {
                        or.BeResourceOwnerForAnyDepartment();
                        or.HaveAnyOrgUnitScopedRole(AccessRoles.ResourceOwner);
                    }
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();
            #endregion

            // Actions have to be filtered based on who is responsible. In the list endpoint
            // we use limited auth to flag that the current user is a task owner and filter
            // actions accordingly.
            var responsible = QueryTaskResponsible.TaskOwner;
            if (!authResult.LimitedAuth)
                responsible = QueryTaskResponsible.ResourceOwner;

            var command = new GetRequestActions(requestId, responsible)
                .WithQuery(query);
            var actions = await DispatchAsync(command);

            return Ok(actions.Select(t => new ApiRequestAction(t)));
        }

        [HttpGet("requests/{requestId}/actions/{actionId}")]
        public async Task<ActionResult> GetRequestAction([FromRoute] Guid requestId, [FromRoute] Guid actionId)
        {
            var request = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));
            if (request is null) return FusionApiError.NotFound(requestId, $"Request with id '{requestId}' was not found.");
            
            var action = await DispatchAsync(new GetRequestAction(requestId, actionId));
            if (action is null) return FusionApiError.NotFound(actionId, $"A task with id '{actionId}' was not found on request with id '{requestId}'.");

            #region Authorization
            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    if(action.Responsible != QueryTaskResponsible.ResourceOwner)
                    {
                        if (request.OrgPositionId.HasValue)
                            or.OrgChartPositionWriteAccess(request.Project.OrgProjectId, request.OrgPositionId.Value);
                        or.BeRequestCreator(requestId);
                    }

                    if (request.AssignedDepartment is not null)
                    {
                        or.BeResourceOwnerForDepartment(
                            new DepartmentPath(request.AssignedDepartment).GoToLevel(2),
                            includeParents: false,
                            includeDescendants: true
                        );
                        or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(request.AssignedDepartment), AccessRoles.ResourceOwner);
                    }
                    else
                    {
                        or.BeResourceOwnerForAnyDepartment();
                        or.HaveAnyOrgUnitScopedRole(AccessRoles.ResourceOwner);
                    }
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();
            #endregion

            return Ok(new ApiRequestAction(action));
        }

        [HttpPatch("requests/{requestId}/actions/{actionId}")]
        public async Task<ActionResult> UpdateRequestAction([FromRoute] Guid requestId, [FromRoute] Guid actionId, [FromBody] PatchActionRequest patch)
        {
            var request = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));
            if (request is null) return FusionApiError.NotFound(requestId, $"Request with id '{requestId}' was not found.");

            var action = await DispatchAsync(new GetRequestAction(requestId, actionId));
            if (action is null) return FusionApiError.NotFound(actionId, $"A task with id '{actionId}' was not found on request with id '{requestId}'.");

            #region Authorization
            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    if (action.Responsible.HasFlag(QueryTaskResponsible.TaskOwner))
                    {
                        if (request.OrgPositionId.HasValue)
                            or.OrgChartPositionWriteAccess(request.Project.OrgProjectId, request.OrgPositionId.Value);
                        or.BeRequestCreator(requestId);
                    }
                    if(action.Responsible.HasFlag(QueryTaskResponsible.ResourceOwner))
                    {
                        if (request.AssignedDepartment is not null)
                        {
                            or.BeResourceOwnerForDepartment(
                                new DepartmentPath(request.AssignedDepartment).GoToLevel(2),
                                includeParents: false,
                                includeDescendants: true
                            );
                            or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(request.AssignedDepartment), AccessRoles.ResourceOwner);
                        }
                        else
                        {
                            or.BeResourceOwnerForAnyDepartment();
                            or.HaveAnyOrgUnitScopedRole(AccessRoles.ResourceOwner);
                        }
                    }
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
            catch (ActionNotFoundError err)
            {
                return FusionApiError.NotFound(actionId, err.Message);
            }
        }

        [HttpDelete("requests/{requestId}/actions/{actionId}")]
        public async Task<ActionResult> DeleteRequestAction([FromRoute] Guid requestId, [FromRoute] Guid actionId)
        {
            var request = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));
            if (request is null) return FusionApiError.NotFound(requestId, $"Request with id '{requestId}' was not found.");
           
            var action = await DispatchAsync(new GetRequestAction(requestId, actionId));
            if (action is null) return FusionApiError.NotFound(actionId, $"A task with id '{actionId}' was not found on request with id '{requestId}'.");

            #region Authorization
            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    r.AlwaysAccessWhen().FullControl().FullControlInternal();
                    r.AnyOf(or =>
                    {
                        r.AnyOf(or =>
                        {
                            if (action.Responsible.HasFlag(QueryTaskResponsible.TaskOwner))
                            {
                                if (request.OrgPositionId.HasValue)
                                    or.OrgChartPositionWriteAccess(request.Project.OrgProjectId, request.OrgPositionId.Value);
                                or.BeRequestCreator(requestId);
                            }
                            if (action.Responsible.HasFlag(QueryTaskResponsible.ResourceOwner))
                            {
                                if (request.AssignedDepartment is not null)
                                {
                                    or.BeResourceOwnerForDepartment(
                                        new DepartmentPath(request.AssignedDepartment).GoToLevel(2),
                                        includeParents: false,
                                        includeDescendants: true
                                    );
                                    or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(request.AssignedDepartment), AccessRoles.ResourceOwner);
                                }
                                else
                                {
                                    or.BeResourceOwnerForAnyDepartment();
                                    or.HaveAnyOrgUnitScopedRole(AccessRoles.ResourceOwner);
                                }
                            }
                        });
                    });
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();
            #endregion

            var command = new DeleteRequestAction(requestId, actionId);
            var wasDeleted = await DispatchAsync(command);

            if (wasDeleted) return NoContent();
            else return Conflict(ProblemDetailsFactory.CreateProblemDetails(HttpContext, title: "Could not delete action, it might already be deleted."));
        }
    }
}
