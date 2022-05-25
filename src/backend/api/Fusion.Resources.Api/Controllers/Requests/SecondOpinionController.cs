using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Authorization;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands;
using Fusion.Resources.Domain.Queries;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Controllers.Requests
{
    [ApiController]
    public class SecondOpinionController : ResourceControllerBase
    {
        [HttpPost("/departments/{departmentString}/resources/requests/{requestId}/second-opinions")]
        public async Task<IActionResult> RequestSecondOpinion(string? departmentString, Guid requestId, [FromBody] AddSecondOpinionRequest payload)
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
                    or.HaveBasicRead(requestId);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var assignedToIds = payload.AssignedTo.Select(x => (PersonId)x);
            var command = new AddSecondOpinion(requestItem.RequestId, payload.Description, assignedToIds);
            var secondOpinion = await DispatchAsync(command);

            return CreatedAtAction(nameof(GetSecondOpinions), new { departmentString = requestItem.AssignedDepartment, requestItem.RequestId }, secondOpinion);
        }

        [HttpGet("/departments/{departmentString}/resources/requests/{requestId}/second-opinions")]
        public async Task<IActionResult> GetSecondOpinions(string? departmentString, Guid requestId)
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
                    or.HaveBasicRead(requestId);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var command = new GetSecondOpinions().WithRequest(requestId);
            var result = await DispatchAsync(command);

            return Ok(result);
        }

        [HttpPatch("/departments/{departmentString}/resources/requests/{requestId}/second-opinions/{secondOpinionId}/responses/{responseId}")]
        public async Task<IActionResult> PatchSecondOpinion()
        {
            throw new NotImplementedException();
        }

        [HttpDelete("/departments/{departmentString}/resources/requests/{requestId}/second-opinions/{secondOpinionId}/responses/{responseId}")]
        public async Task<IActionResult> UnassignSecondOpinion()
        {
            throw new NotImplementedException();
        }

        [HttpGet("/persons/{personId}/second-opinions/responses")]
        public async Task<IActionResult> GetPersonalResponses(string personId)
        {
            PersonId assigneeId = personId switch
            {
                "me" => User.GetAzureUniqueIdOrThrow(),
                _ => new PersonId(personId)
            };

            var command = new GetSecondOpinions().WithAssignee(assigneeId);
            var result = await DispatchAsync(command);

            return Ok(result);
        }

        [HttpGet("/persons/{personId}/second-opinions/")]
        public async Task<IActionResult> GetPersonalSecondOpinions(string personId)
        {
            PersonId creatorId = personId switch
            {
                "me" => User.GetAzureUniqueIdOrThrow(),
                _ => new PersonId(personId)
            };

            var command = new GetSecondOpinions().WithCreator(creatorId);
            var result = await DispatchAsync(command);

            return Ok(result);
        }
    }
}
