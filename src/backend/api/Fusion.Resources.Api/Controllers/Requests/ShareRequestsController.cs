using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Authorization;
using Fusion.Integration.Profile;
using Fusion.Resources.Api.Controllers.Requests.Requests;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands.Requests.Sharing;
using Fusion.Resources.Domain.Models;
using Fusion.Resources.Domain.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Controllers.Requests
{
    [ApiController]
    public class ShareRequestsController : ResourceControllerBase
    {
        [HttpPost("/resources/requests/internal/{requestId}/share")]
        public async Task<IActionResult> ShareRequest(Guid requestId, ShareRequestRequest request)
        {
            var requestItem = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));
            if (requestItem is null) return FusionApiError.NotFound(requestId, $"Request with id '{requestId}' was not found.");

            var isSuccess = await DispatchAsync(request.ToCommand(requestId));

            return isSuccess ? Ok() : Conflict();
        }

        [HttpGet("/persons/{personId}/shared-requests")]
        public async Task<ActionResult<IEnumerable<ApiResourceAllocationRequest>>> GetSharedRequests(string personId)
        {
            Guid azureId;
            if (personId == "me")
            {
                azureId = User.GetAzureUniqueIdOrThrow();
            }
            else if (!Guid.TryParse(personId, out azureId))
            {
                return FusionApiError.InvalidOperation("InvalidUserId", $"The supplied value '{personId}' is not a valid user id.");
            }

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen()
                    .FullControl()
                    .FullControlInternal()
                    .BeTrustedApplication();

                r.AnyOf(or => or.CurrentUserIs(new PersonIdentifier(azureId)));
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var requests = await DispatchAsync(
                new GetResourceAllocationRequests().ForAll().SharedWith(azureId)
            );

            return Ok(new ApiCollection<ApiResourceAllocationRequest>(requests.Select(x => new ApiResourceAllocationRequest(x))));
        }
    }
}
