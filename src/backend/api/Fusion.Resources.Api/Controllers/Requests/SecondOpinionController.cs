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
        [HttpPost("/resources/requests/internal/{requestId}/second-opinions")]
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

            return CreatedAtAction(nameof(GetSecondOpinions), new { departmentString = requestItem.AssignedDepartment, requestItem.RequestId }, new ApiSecondOpinion(secondOpinion));
        }

        [HttpGet("/resources/requests/internal/{requestId}/second-opinions")]
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

            return Ok(result.Select(x => new ApiSecondOpinion(x)));
        }

        [HttpPatch("/resources/requests/internal/{requestId}/second-opinions/{secondOpinionId}/")]
        public async Task<IActionResult> PatchSecondOpinion(Guid requestId, Guid secondOpinionId, [FromBody] PatchSecondOpinionRequest payload)
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

            var command = new UpdateSecondOpinion(secondOpinionId);

            if (payload.Description.HasValue)
            {
                command.Description = payload.Description.Value;
            }

            if (payload.AssignedTo.HasValue)
            {
                command.AssignedTo = payload.AssignedTo.Value.Select(x => (PersonId)x).ToList();
            }

            var secondOpinion = await DispatchAsync(command);
            if (secondOpinion is null)
                return ApiErrors.NotFound("Could not locate second opinon");


            return Ok(new ApiSecondOpinion(secondOpinion));
        }

        [HttpPatch("/resources/requests/internal/{requestId}/second-opinions/{secondOpinionId}/responses/{responseId}")]
        public async Task<IActionResult> PatchSecondOpinionResponse(Guid requestId, Guid secondOpinionId, Guid responseId, PatchSecondOpinionResponseRequest payload)
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

            var command = new UpdateSecondOpinionResponse(secondOpinionId, responseId);
            var response = await DispatchAsync(command);

            if (response is null)
                return ApiErrors.NotFound("Could not locate second opinion");

            return Ok(new ApiSecondOpinionResponse(response));
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
