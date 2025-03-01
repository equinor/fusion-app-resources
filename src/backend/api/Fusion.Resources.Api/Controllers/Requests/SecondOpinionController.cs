﻿using Fusion.AspNetCore.FluentAuthorization;
using Fusion.AspNetCore.OData;
using Fusion.Authorization;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands;
using Fusion.Resources.Domain.Queries;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Controllers.Requests
{
    [ApiController]
    public class SecondOpinionController : ResourceControllerBase
    {
        [EmulatedUserSupport]
        [HttpOptions("/resources/requests/internal/{requestId}/second-opinions")]
        public async Task<IActionResult> CheckSecondOpinionAccess(Guid requestId)
        {
            var requestItem = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));

            if (requestItem == null)
                return ApiErrors.NotFound("Could not locate request", $"{requestId}");

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal().BeTrustedApplication();
                r.AnyOf(or =>
                {
                    if (requestItem.AssignedDepartment is not null)
                    {
                        or.BeResourceOwnerForDepartment(
                            new DepartmentPath(requestItem.AssignedDepartment).GoToLevel(2),
                            includeParents: false,
                            includeDescendants: true
                        );
                    }
                    else
                    {
                        or.BeResourceOwnerForAnyDepartment();
                        or.HaveAnyOrgUnitScopedRole(AccessRoles.ResourceOwner);
                    }
                });

                r.LimitedAccessWhen(or => or.HaveBasicRead(requestId));
            });

            var allowed = new List<string>();

            if (authResult.Success)
            {
                allowed.Add("GET");
                if (!authResult.LimitedAuth && !requestItem.IsCompleted) allowed.Add("POST");
            }

            Response.Headers.Add("Allow", string.Join(',', allowed));
            return NoContent();
        }

        [HttpPost("/resources/requests/internal/{requestId}/second-opinions")]
        public async Task<ActionResult<ApiSecondOpinion>> RequestSecondOpinion(Guid requestId, [FromBody] AddSecondOpinionRequest payload)
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
                        or.BeResourceOwnerForDepartment(
                            new DepartmentPath(requestItem.AssignedDepartment).GoToLevel(2),
                            includeParents: false,
                            includeDescendants: true
                        );
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

            if (requestItem.IsCompleted)
                return ApiErrors.InvalidOperation("SecondOpinionForClosedRequest", "Cannot request second opinions for completed requests");

            var assignedToIds = payload.AssignedTo.Select(x => (PersonId)x);
            var command = new AddSecondOpinion(requestItem.RequestId, payload.Title, payload.Description, assignedToIds);
            var secondOpinion = await DispatchAsync(command);

            return CreatedAtAction(nameof(RequestSecondOpinion), new { requestItem.RequestId }, new ApiSecondOpinion(secondOpinion, User.GetAzureUniqueIdOrThrow()));
        }

        [HttpGet("/resources/requests/internal/{requestId}/second-opinions")]
        public async Task<ActionResult<List<ApiSecondOpinion>>> GetSecondOpinions(Guid requestId)
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
                        or.BeResourceOwnerForDepartment(
                            new DepartmentPath(requestItem.AssignedDepartment).GoToLevel(2),
                            includeParents: false,
                            includeDescendants: true
                        );
                    }
                    else
                    {
                        or.BeResourceOwnerForAnyDepartment();
                        or.HaveAnyOrgUnitScopedRole(AccessRoles.ResourceOwner);
                    }
                    or.HaveBasicRead(requestId);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var command = new GetSecondOpinions().WithRequest(requestId);
            var result = await DispatchAsync(command);


            return Ok(new ApiSecondOpinionResult(result, User.GetAzureUniqueIdOrThrow()));
        }

        [EmulatedUserSupport]
        [HttpOptions("/resources/requests/internal/{requestId}/second-opinions/{secondOpinionId}")]
        public async Task<IActionResult> CheckSecondOpinionAccess(Guid requestId, Guid secondOpinionId)
        {
            var requestItem = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));

            if (requestItem == null)
                return ApiErrors.NotFound("Could not locate request", $"{requestId}");

            var secondOpinion = (await DispatchAsync(new GetSecondOpinions()
                .WithRequest(requestId)
                .WithId(secondOpinionId)
            )).Value.SingleOrDefault();

            if (secondOpinion == null)
                return ApiErrors.NotFound("Could not locate second opinion", $"{secondOpinionId}");


            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal().BeTrustedApplication();
                r.AnyOf(or =>
                {
                    or.CurrentUserIs(secondOpinion.CreatedBy.AzureUniqueId);
                });
            });

            var allowed = new List<string>();

            if (authResult.Success && !requestItem.IsCompleted)
            {
                allowed.Add("PATCH", "DELETE");
            }

            Response.Headers.Add("Allow", string.Join(',', allowed));
            return NoContent();
        }

        [HttpDelete("/resources/requests/internal/{requestId}/second-opinions/{secondOpinionId}")]
        public async Task<IActionResult> DeleteSecondOpinion(Guid requestId, Guid secondOpinionId)
        {
            var requestItem = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));

            if (requestItem == null)
                return ApiErrors.NotFound("Could not locate request", $"{requestId}");

            if (requestItem.IsCompleted)
                return ApiErrors.InvalidOperation("RequestCompleted", "Cannot delete second opinion when request is completed.");

            var secondOpinion = (await DispatchAsync(new GetSecondOpinions()
                .WithRequest(requestId)
                .WithId(secondOpinionId)
            )).Value.SingleOrDefault();

            if (secondOpinion is null)
                return ApiErrors.NotFound("Could not locate second opinion for request", $"{secondOpinionId}");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal().BeTrustedApplication();
                r.AnyOf(or => or.CurrentUserIs(secondOpinion.CreatedBy.AzureUniqueId));
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            await DispatchAsync(new DeleteSecondOpinion(secondOpinionId));

            return NoContent();
        }

        [HttpPatch("/resources/requests/internal/{requestId}/second-opinions/{secondOpinionId}")]
        public async Task<ActionResult<ApiSecondOpinion>> PatchSecondOpinion(Guid requestId, Guid secondOpinionId, [FromBody] PatchSecondOpinionRequest payload)
        {
            var requestItem = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));

            if (requestItem == null)
                return ApiErrors.NotFound("Could not locate request", $"{requestId}");

            var secondOpinion = (await DispatchAsync(new GetSecondOpinions()
                .WithRequest(requestId)
                .WithId(secondOpinionId)
            )).Value.SingleOrDefault();

            if (secondOpinion is null)
                return ApiErrors.NotFound("Could not locate second opinion for request", $"{secondOpinionId}");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal().BeTrustedApplication();
                r.AnyOf(or => or.CurrentUserIs(secondOpinion.CreatedBy.AzureUniqueId));
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var command = new UpdateSecondOpinion(secondOpinionId);

            if (payload.Title.HasValue)
            {
                command.Title = payload.Title.Value;
            }

            if (payload.Description.HasValue)
            {
                command.Description = payload.Description.Value;
            }

            if (payload.AssignedTo.HasValue)
            {
                command.AssignedTo = payload.AssignedTo.Value.Select(x => (PersonId)x).ToList();
            }

            secondOpinion = await DispatchAsync(command);
            return Ok(new ApiSecondOpinion(secondOpinion!, User.GetAzureUniqueIdOrThrow()));
        }

        [EmulatedUserSupport]
        [HttpOptions("/resources/requests/internal/{requestId}/second-opinions/{secondOpinionId}/responses/{responseId}")]
        public async Task<ActionResult<ApiSecondOpinionResponse>> CheckPatchSecondOpinionResponse(Guid requestId, Guid secondOpinionId, Guid responseId)
        {
            var requestItem = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));

            if (requestItem == null)
                return ApiErrors.NotFound("Could not locate request", $"{requestId}");

            var result = await DispatchAsync(new GetSecondOpinions().WithRequest(requestId).WithId(secondOpinionId));
            
            var secondOpinion = result.SingleOrDefault();
            if (secondOpinion == null)
                return ApiErrors.NotFound("Could not locate second opinion");

            var response = secondOpinion.Responses.FirstOrDefault(x => x.Id == responseId);
            if (response == null)
                return ApiErrors.NotFound("Could not locate response on second opinion");

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal().BeTrustedApplication();
                r.AnyOf(or => or.CurrentUserIs(response.AssignedTo.AzureUniqueId));
            });

            if (authResult.Success)
                Response.Headers["Allow"] = "PATCH, DELETE";

            return NoContent();
        }

        [HttpPatch("/resources/requests/internal/{requestId}/second-opinions/{secondOpinionId}/responses/{responseId}")]
        public async Task<ActionResult<ApiSecondOpinionResponse>> PatchSecondOpinionResponse(Guid requestId, Guid secondOpinionId, Guid responseId, PatchSecondOpinionResponseRequest payload)
        {
            var requestItem = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));

            if (requestItem == null)
                return ApiErrors.NotFound("Could not locate request", $"{requestId}");

            var result = await DispatchAsync(new GetSecondOpinions().WithRequest(requestId).WithId(secondOpinionId));

            var secondOpinion = result.SingleOrDefault();
            if (secondOpinion == null)
                return ApiErrors.NotFound("Could not locate second opinion");

            var response = secondOpinion.Responses.FirstOrDefault(x => x.Id == responseId);
            if (response == null)
                return ApiErrors.NotFound("Could not locate response on second opinion");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal().BeTrustedApplication();
                r.AnyOf(or => or.CurrentUserIs(response.AssignedTo.AzureUniqueId));
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var command = new UpdateSecondOpinionResponse(secondOpinionId, responseId);

            if (payload.Comment.HasValue) command.Comment = payload.Comment.Value;
            if (payload.State.HasValue)
            {
                command.State = payload.State.Value switch
                {
                    ApiSecondOpinionResponseStates.Open => QuerySecondOpinionResponseStates.Open,
                    ApiSecondOpinionResponseStates.Draft => QuerySecondOpinionResponseStates.Draft,
                    ApiSecondOpinionResponseStates.Published => QuerySecondOpinionResponseStates.Published,
                    _ => throw new NotImplementedException()
                };
            }

            response = await DispatchAsync(command);

            return Ok(new ApiSecondOpinionResponse(response!, User.GetAzureUniqueIdOrThrow()));
        }


        [HttpDelete("/resources/requests/internal/{requestId}/second-opinions/{secondOpinionId}/responses/{responseId}")]
        public async Task<ActionResult<ApiSecondOpinionResponse>> DeleteSecondOpinionResponse(Guid requestId, Guid secondOpinionId, Guid responseId)
        {
            var requestItem = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));

            if (requestItem == null)
                return ApiErrors.NotFound("Could not locate request", $"{requestId}");

            var secondOpinion = (await DispatchAsync(new GetSecondOpinions().WithRequest(requestId).WithId(secondOpinionId))).Value.SingleOrDefault();
            if (secondOpinion == null)
                return ApiErrors.NotFound("Could not locate second opinion");

            var response = secondOpinion.Responses.FirstOrDefault(x => x.Id == responseId);
            if (response == null)
                return ApiErrors.NotFound("Could not locate response on second opinion");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal().BeTrustedApplication();
                r.AnyOf(or => or.CurrentUserIs(response.AssignedTo.AzureUniqueId));
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var command = new DeleteSecondOpinionResponse(secondOpinionId, responseId);

            return NoContent();
        }

        [EmulatedUserSupport]
        [HttpOptions("/persons/{personId}/second-opinions/")]
        [HttpOptions("/persons/{personId}/second-opinions/responses")]
        public async Task<IActionResult> CheckPersonalAccess(string personId)
        {
            PersonId assigneeId = personId switch
            {
                "me" => User.GetAzureUniqueIdOrThrow(),
                _ => new PersonId(personId)
            };

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal().BeTrustedApplication();
                r.AnyOf(or => or.CurrentUserIs(assigneeId));
            });

            if (authResult.Success) Response.Headers["Allow"] = "GET";
            return NoContent();
        }

        [HttpGet("/persons/{personId}/second-opinions/responses")]
        public async Task<ActionResult<List<ApiSecondOpinion>>> GetPersonalResponses(string personId, [FromQuery] ODataQueryParams query)
        {
            PersonId assigneeId = personId switch
            {
                "me" => User.GetAzureUniqueIdOrThrow(),
                _ => new PersonId(personId)
            };

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal().BeTrustedApplication();
                r.AnyOf(or => or.CurrentUserIs(assigneeId));
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            var command = new GetSecondOpinions().WithAssignee(assigneeId).WithQuery(query);
            var result = await DispatchAsync(command);

            return Ok(new ApiSecondOpinionResult(result, assigneeId.UniqueId!.Value));
        }

        [HttpGet("/persons/{personId}/second-opinions/")]
        public async Task<ActionResult<List<ApiSecondOpinion>>> GetPersonalSecondOpinions(string personId, [FromQuery] ODataQueryParams query)
        {
            var countEnabled = Request.Query.ContainsKey("$count");

            PersonId creatorId = personId switch
            {
                "me" => User.GetAzureUniqueIdOrThrow(),
                _ => new PersonId(personId)
            };

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal().BeTrustedApplication();
                r.AnyOf(or => or.CurrentUserIs(creatorId));
            });

            var command = new GetSecondOpinions()
                .WithCreator(creatorId)
                .WithQuery(query)
                .WithCountOnly(countEnabled);
            var result = await DispatchAsync(command);


            return Ok(new ApiSecondOpinionResult(result, User.GetAzureUniqueIdOrThrow()));
        }
    }
}
