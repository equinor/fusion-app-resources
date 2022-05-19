using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Authorization;
using Fusion.Integration.Profile;
using Fusion.Resources.Api.Controllers.Requests.Requests;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands.Requests.Sharing;
using Fusion.Resources.Domain.Queries;
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

            if (requestItem == null)
                return ApiErrors.NotFound("Could not locate request", $"{requestId}");

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
            
            var isSuccess = await DispatchAsync(request.ToCommand(requestId));

            return isSuccess ? Ok() : Conflict();
        }

        [HttpGet("resources/persons/{personId}/requests/shared")]
        public async Task<ActionResult<IEnumerable<ApiResourceAllocationRequest>>> GetSharedRequests(string personId)
        {
            PersonId azureId;
            if (personId == "me")
            {
                azureId = User.GetAzureUniqueIdOrThrow();
            }
            else
            {
                azureId = new PersonId(personId);
            }
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen()
                    .FullControl()
                    .FullControlInternal()
                    .BeTrustedApplication();

                r.AnyOf(or => or.CurrentUserIs(azureId));
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var requests = await DispatchAsync(
                new GetResourceAllocationRequests().ForAll().SharedWith(azureId)
            );

            return Ok(new ApiCollection<ApiResourceAllocationRequest>(requests.Select(x => new ApiResourceAllocationRequest(x))));
        }

        [HttpDelete("/resources/requests/internal/{requestId}/share/{sharedToAzureId}")]
        public async Task<IActionResult> DeleteSharedRequests(Guid requestId, string sharedToAzureId)
        {
            var requestItem = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));

            if (requestItem == null)
                return ApiErrors.NotFound("Could not locate request", $"{requestId}");

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

            var wasDeleted = await DispatchAsync(new RevokeShareRequest(requestId, new PersonId(sharedToAzureId), SharedRequestSource.User));

            return wasDeleted != null ? Ok() : NotFound();
        }
    }
}
