using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Authorization;
using Fusion.Integration.LineOrg;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands.Conversations;
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
    public class ConversationsController : ResourceControllerBase
    {
        [HttpPost("/projects/{projectIdentifier}/requests/{requestId}/conversation")]
        [HttpPost("/projects/{projectIdentifier}/resources/requests/{requestId}/conversation")]
        public async Task<ActionResult> AddConversationMessage([FromRoute] Guid requestId, string? departmentString, [FromBody] AddRequestConversationMessageRequest request)
        {
            var requestItem = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));
            if (requestItem is null) return FusionApiError.NotFound(requestId, $"Request with id '{requestId}' was not found.");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    if (requestItem.AssignedDepartment is not null)
                    {
                        or.BeResourceOwner(
                            new DepartmentPath(requestItem.AssignedDepartment).GoToLevel(2),
                            includeParents: false,
                            includeDescendants: true
                        );
                        or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(requestItem.AssignedDepartment), Roles.ResourceOwner);
                    }
                    else
                    {
                        or.BeResourceOwner();
                    }
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();
            #endregion

            var recipientType = request.Recipient.ToDomain();

            var command = new AddRequestConversationMessage(requestId, request.Title, request.Body, request.Category, recipientType)
            {
                Properties = request.Properties
            };

            var created = await DispatchAsync(command);
            return CreatedAtAction(nameof(GetRequestConversation), new { requestId, messageId = created.Id }, new ApiRequestConversationMessage(created));
        }

        [HttpPost("/requests/internal/{requestId}/conversation")]
        [HttpPost("/departments/{departmentString}/resources/requests/{requestId}/conversation")]
        public async Task<ActionResult> AddConversationMessage([FromRoute] Guid requestId, Guid? projectIdentifier, string? departmentString, [FromBody] AddRequestConversationMessageRequest request)
        {
            var requestItem = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));
            if (requestItem is null) return FusionApiError.NotFound(requestId, $"Request with id '{requestId}' was not found.");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    if (requestItem.AssignedDepartment is not null)
                    {
                        or.BeResourceOwner(
                            new DepartmentPath(requestItem.AssignedDepartment).GoToLevel(2),
                            includeParents: false,
                            includeDescendants: true
                        );
                        or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(requestItem.AssignedDepartment), Roles.ResourceOwner);
                    }
                    else
                    {
                        or.BeResourceOwner();
                    }
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();
            #endregion

            var recipientType = request.Recipient.ToDomain();

            var command = new AddRequestConversationMessage(requestId, request.Title, request.Body, request.Category, recipientType)
            {
                Properties = request.Properties
            };

            var created = await DispatchAsync(command);
            return CreatedAtAction(nameof(GetRequestConversation), new { requestId, messageId = created.Id }, new ApiRequestConversationMessage(created));
        }

        [HttpGet("/projects/{projectIdentifier}/requests/{requestId}/conversation")]
        [HttpGet("/projects/{projectIdentifier}/resources/requests/{requestId}/conversation")]
        public async Task<ActionResult> GetRequestConversation(Guid requestId, Guid? projectIdentifier)
        {
            var requestItem = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));
            if (requestItem is null) return FusionApiError.NotFound(requestId, $"Request with id '{requestId}' was not found.");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal().BeTrustedApplication();
                r.AnyOf(or =>
                {
                    if (requestItem.OrgPositionId.HasValue)
                        or.OrgChartPositionWriteAccess(requestItem.Project.OrgProjectId, requestItem.OrgPositionId.Value);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var conversation = await DispatchAsync(new GetRequestConversation(requestId, QueryMessageRecipient.TaskOwner));
            return Ok(conversation.Select(x => new ApiRequestConversationMessage(x)));
        }

        [HttpGet("/requests/internal/{requestId}/conversation")]
        [HttpGet("/departments/{departmentString}/resources/requests/{requestId}/conversation")]
        public async Task<ActionResult> GetRequestConversation(Guid requestId, string? departmentString)
        {
            var requestItem = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));
            if (requestItem is null) return FusionApiError.NotFound(requestId, $"Request with id '{requestId}' was not found.");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal().BeTrustedApplication();

                r.AnyOf(or =>
                {
                    if (requestItem.AssignedDepartment is not null)
                    {
                        or.BeResourceOwner(
                            new DepartmentPath(requestItem.AssignedDepartment).GoToLevel(2),
                            includeParents: false,
                            includeDescendants: true
                        );
                        or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(requestItem.AssignedDepartment), Roles.ResourceOwner);
                    }
                    or.BeResourceOwner();
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var conversation = await DispatchAsync(new GetRequestConversation(requestId, QueryMessageRecipient.ResourceOwner));
            return Ok(conversation.Select(x => new ApiRequestConversationMessage(x)));
        }
        [HttpGet("/projects/{projectIdentifier}/requests/{requestId}/conversation/{messageId}")]
        [HttpGet("/projects/{projectIdentifier}/resources/requests/{requestId}/conversation/{messageId}")]
        public async Task<ActionResult> GetRequestConversation(Guid requestId, Guid messageId, Guid? projectIdentifier)
        {
            var requestItem = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));
            if (requestItem is null) return FusionApiError.NotFound(requestId, $"Request with id '{requestId}' was not found.");

            var conversation = await DispatchAsync(new GetRequestConversationMessage(requestId, messageId));
            if (conversation is null) return FusionApiError.NotFound(messageId, $"Message with id '{messageId}' was not found.");

            #region Authorization
            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal().BeTrustedApplication();
                r.Must(and =>
                {
                    and.RequireConversationForTaskOwner(conversation.Recipient);
                    if (requestItem.OrgPositionId.HasValue)
                        and.OrgChartPositionWriteAccess(requestItem.Project.OrgProjectId, requestItem.OrgPositionId.Value);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();
            #endregion

            return Ok(new ApiRequestConversationMessage(conversation));
        }

        [HttpGet("/requests/internal/{requestId}/conversation/{messageId}")]
        [HttpGet("/departments/{departmentString}/resources/requests/{requestId}/conversation/{messageId}")]
        public async Task<ActionResult> GetRequestConversation(Guid requestId, Guid messageId, string? departmentString)
        {
            var requestItem = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));
            if (requestItem is null) return FusionApiError.NotFound(requestId, $"Request with id '{requestId}' was not found.");

            var conversation = await DispatchAsync(new GetRequestConversationMessage(requestId, messageId));
            if (conversation is null) return FusionApiError.NotFound(messageId, $"Message with id '{messageId}' was not found.");

            #region Authorization
            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal().BeTrustedApplication();
                r.Must(and =>
                {
                    and.RequireConversationForResourceOwner(conversation.Recipient);
                    if (requestItem.AssignedDepartment is not null)
                    {
                        and.BeResourceOwner(
                            new DepartmentPath(requestItem.AssignedDepartment).GoToLevel(2),
                            includeParents: false,
                            includeDescendants: true
                        );
                        and.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(requestItem.AssignedDepartment), Roles.ResourceOwner);
                    }
                    else
                    {
                        and.BeResourceOwner();
                    }
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();
            #endregion

            return Ok(new ApiRequestConversationMessage(conversation));
        }
        [HttpPut("/projects/{projectIdentifier}/requests/{requestId}/conversation/{messageId}")]
        [HttpPut("/projects/{projectIdentifier}/resources/requests/{requestId}/conversation/{messageId}")]
        public async Task<ActionResult> UpdateRequestConversation(Guid requestId, Guid messageId, Guid? projectIdentifier, string? departmentString, [FromBody] UpdateRequestConversationMessageRequest request)
        {
            var requestItem = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));
            if (requestItem is null) return FusionApiError.NotFound(requestId, $"Request with id '{requestId}' was not found.");

            var conversation = await DispatchAsync(new GetRequestConversationMessage(requestId, messageId));
            if (conversation is null) return FusionApiError.NotFound(messageId, $"Message with id '{messageId}' was not found.");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal().BeTrustedApplication();
                r.Must(and =>
                {
                    and.RequireConversationForTaskOwner(conversation.Recipient);
                    if (requestItem.OrgPositionId.HasValue)
                        and.OrgChartPositionWriteAccess(requestItem.Project.OrgProjectId, requestItem.OrgPositionId.Value);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();
            #endregion

            var recipientType = request.Recipient.ToDomain();

            var command = new UpdateRequestConversationMessage(requestId, messageId, request.Title, request.Body, request.Category, recipientType)
            {
                Properties = request.Properties
            };

            var updated = await DispatchAsync(command);

            return Ok(new ApiRequestConversationMessage(updated!));
        }

        [HttpPut("/requests/internal/{requestId}/conversation/{messageId}")]
        [HttpPut("/departments/{departmentString}/resources/requests/{requestId}/conversation/{messageId}")]
        public async Task<ActionResult> UpdateRequestConversation(Guid requestId, Guid messageId, string? departmentString, [FromBody] UpdateRequestConversationMessageRequest request)
        {
            var requestItem = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));
            if (requestItem is null) return FusionApiError.NotFound(requestId, $"Request with id '{requestId}' was not found.");

            var conversation = await DispatchAsync(new GetRequestConversationMessage(requestId, messageId));
            if (conversation is null) return FusionApiError.NotFound(messageId, $"Message with id '{messageId}' was not found.");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal().BeTrustedApplication();
                r.Must(and =>
                {
                    and.RequireConversationForResourceOwner(conversation.Recipient);
                    if (requestItem.AssignedDepartment is not null)
                    {
                        and.BeResourceOwner(
                            new DepartmentPath(requestItem.AssignedDepartment).GoToLevel(2),
                            includeParents: false,
                            includeDescendants: true
                        );
                        and.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(requestItem.AssignedDepartment), Roles.ResourceOwner);
                    }
                    else
                    {
                        and.BeResourceOwner();
                    }
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();
            #endregion

            var recipientType = request.Recipient.ToDomain();

            var command = new UpdateRequestConversationMessage(requestId, messageId, request.Title, request.Body, request.Category, recipientType)
            {
                Properties = request.Properties
            };

            var updated = await DispatchAsync(command);

            return Ok(new ApiRequestConversationMessage(updated!));
        }
    }
}
