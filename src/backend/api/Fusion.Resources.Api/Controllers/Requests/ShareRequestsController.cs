using Fusion.Resources.Api.Controllers.Requests.Requests;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands.Requests.Sharing;
using Fusion.Resources.Domain.Models;
using Fusion.Resources.Domain.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
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
    }
}
