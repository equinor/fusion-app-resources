using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands.Conversations;
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
    public class ConversationsController : ResourceControllerBase
    {
        [HttpPost("/requests/internal/{requestId}/conversation")]
        public async Task<ActionResult> AddConversationMessage([FromRoute] Guid requestId, [FromBody] AddRequestConversationMessageRequest request)
        {
            var requestItem = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));
            if (requestItem is null) return FusionApiError.NotFound(requestId, $"Request with id '{requestId}' was not found.");

            var recipientType = request.Recipient switch
            {
                ApiMessageRecipient.ResourceOwner => QueryMessageRecipient.ResourceOwner,
                ApiMessageRecipient.TaskOwner => QueryMessageRecipient.TaskOwner,
                ApiMessageRecipient.Both => QueryMessageRecipient.Both,
                _ => throw new NotSupportedException($"Recipient type '{request.Recipient}' is not supported.")
            };

            var command = new AddRequestConversationMessage(requestId, request.Title, request.Body, request.Category, recipientType)
            {
                Properties = request.Properties
            };

            var created = await DispatchAsync(command);
            return CreatedAtAction(nameof(GetRequestConversation), new { requestId, messageId = created.Id }, new ApiRequestConversationMessage(created));
        }

        [HttpGet("/requests/internal/{requestId}/conversation")]
        public async Task<ActionResult> GetRequestConversation(Guid requestId)
        {
            var requestItem = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));
            if (requestItem is null) return FusionApiError.NotFound(requestId, $"Request with id '{requestId}' was not found.");

            var conversation = await DispatchAsync(new GetRequestConversation(requestId));
            return Ok(conversation.Select(x => new ApiRequestConversationMessage(x)));
        }

        [HttpGet("/requests/internal/{requestId}/conversation/{messageId}")]
        public async Task<ActionResult> GetRequestConversation(Guid requestId, Guid messageId)
        {
            var requestItem = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));
            if (requestItem is null) return FusionApiError.NotFound(requestId, $"Request with id '{requestId}' was not found.");

            var conversation = await DispatchAsync(new GetRequestConversationMessage(requestId, messageId));

            if (conversation is null) return FusionApiError.NotFound(messageId, $"Message with id '{messageId}' was not found.");

            return Ok(new ApiRequestConversationMessage(conversation));
        }

        [HttpPut("/requests/internal/{requestId}/conversation/{messageId}")]
        public async Task<ActionResult> UpdateRequestConversation(Guid requestId, Guid messageId, [FromBody] UpdateRequestConversationMessageRequest request)
        {
            var requestItem = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));
            if (requestItem is null) return FusionApiError.NotFound(requestId, $"Request with id '{requestId}' was not found.");

            var recipientType = request.Recipient switch
            {
                ApiMessageRecipient.ResourceOwner => QueryMessageRecipient.ResourceOwner,
                ApiMessageRecipient.TaskOwner => QueryMessageRecipient.TaskOwner,
                ApiMessageRecipient.Both => QueryMessageRecipient.Both,
                _ => throw new NotSupportedException($"Recipient type '{request.Recipient}' is not supported.")
            };

            var command = new UpdateRequestConversationMessage(requestId, messageId, request.Title, request.Body, request.Category, recipientType)
            {
                Properties = request.Properties
            };

            var conversation = await DispatchAsync(command);

            if (conversation is null) return FusionApiError.NotFound(messageId, $"Message with id '{messageId}' was not found.");

            return Ok(new ApiRequestConversationMessage(conversation));
        }
    }
}
