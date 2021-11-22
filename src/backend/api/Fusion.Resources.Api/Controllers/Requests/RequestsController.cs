using Fusion.AspNetCore.FluentAuthorization;
using Fusion.AspNetCore.OData;
using Fusion.Authorization;
using Fusion.Resources.Api.Authorization;
using Fusion.Resources.Domain.Commands;
using Fusion.Resources.Domain.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using ContractRole = Fusion.Resources.Api.Authorization.ContractRole;

namespace Fusion.Resources.Api.Controllers
{
    [Authorize]
    [ApiController]
    public class RequestsController : ResourceControllerBase
    {
        /// <summary>
        /// 
        /// OData:
        ///     $expand = originalPosition
        ///     
        /// </summary>
        /// <param name="projectIdentifier"></param>
        /// <param name="contractIdentifier"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        [HttpGet("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests")]
        public async Task<ActionResult<ApiCollection<ApiContractPersonnelRequest>>> GetContractRequests([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, [FromQuery] ODataQueryParams query)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlExternal();

                r.AnyOf(or =>
                {
                    or.ContractAccess(ContractRole.Any, projectIdentifier, contractIdentifier);
                    or.DelegatedContractAccess(DelegatedContractRole.Any, projectIdentifier, contractIdentifier);
                    or.BeContractorInContract(contractIdentifier);
                    or.BeTrustedApplication();
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion


            var requests = await DispatchAsync(GetContractPersonnelRequests.QueryContract(projectIdentifier.ProjectId, contractIdentifier).WithQuery(query));

            return new ApiCollection<ApiContractPersonnelRequest>(requests.Select(r => new ApiContractPersonnelRequest(r)));
        }

        [HttpGet("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests/{requestId}")]
        public async Task<ActionResult<ApiContractPersonnelRequest>> GetContractRequestById([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid requestId, [FromQuery] ODataQueryParams query)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlExternal();

                r.AnyOf(or =>
                {
                    or.ContractAccess(ContractRole.Any, projectIdentifier, contractIdentifier);
                    or.DelegatedContractAccess(DelegatedContractRole.Any, projectIdentifier, contractIdentifier);
                    or.BeContractorInContract(contractIdentifier);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var request = await DispatchAsync(new GetContractPersonnelRequest(requestId).WithQuery(query));
            return new ApiContractPersonnelRequest(request);
        }


        /// <summary>
        /// 
        /// 
        /// Validations:
        /// - Only one change request for a specific position can be active at the same time.
        ///    -> Bad Request, Invalid operation.
        ///    
        /// - The original position id has to be a valid position.
        ///     -> Bad Request
        /// </summary>
        /// <param name="projectIdentifier"></param>
        /// <param name="contractIdentifier"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests")]
        public async Task<ActionResult<ApiContractPersonnelRequest>> CreatePersonnelRequest([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, [FromBody] ContractPersonnelRequestRequest request)
        {

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlExternal();

                r.AnyOf(or =>
                {
                    or.ContractAccess(ContractRole.Any, projectIdentifier, contractIdentifier);
                    or.DelegatedContractAccess(DelegatedContractRole.Any, projectIdentifier, contractIdentifier);
                    or.BeContractorInContract(contractIdentifier);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion


            try
            {
                using (var scope = await BeginTransactionAsync())
                {
                    var createCommand = new Logic.Commands.ContractorPersonnelRequest.Create(projectIdentifier.ProjectId, contractIdentifier, request.Person)
                        .WithOriginalPosition(request.OriginalPositionId)
                        .WithPosition(request.Position.BasePosition.Id, request.Position.Name, request.Position.AppliesFrom, request.Position.AppliesTo, request.Position.Workload, request.Position.Obs)
                        .WithTaskOwner(request.Position.TaskOwner?.PositionId)
                        .WithDescription(request.Description);

                    var query = await DispatchAsync(createCommand);

                    await scope.CommitAsync();

                    return new ApiContractPersonnelRequest(query);
                }
            }
            catch (InvalidOrgChartPositionError ex)
            {
                return ApiErrors.InvalidOperation(ex);
            }
            catch (InvalidOperationException ex)
            {
                return ApiErrors.InvalidOperation(ex);
            }
        }

        [HttpPut("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests/{requestId}")]
        public async Task<ActionResult<ApiContractPersonnelRequest>> UpdatePersonnelRequest([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid requestId, [FromBody] ContractPersonnelRequestRequest request)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlExternal();

                r.AnyOf(or =>
                {
                    or.ContractAccess(ContractRole.Any, projectIdentifier, contractIdentifier);
                    or.DelegatedContractAccess(DelegatedContractRole.Any, projectIdentifier, contractIdentifier);
                    or.BeContractorInContract(contractIdentifier);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            using (var scope = await BeginTransactionAsync())
            {
                var query = await DispatchAsync(new Logic.Commands.ContractorPersonnelRequest.Update(requestId)
                    .SetPerson(request.Person)
                    .SetDescription(request.Description)
                    .SetTaskOwner(request.Position.TaskOwner?.PositionId)
                    .SetPosition(request.Position.BasePosition.Id, request.Position.Name, request.Position.AppliesFrom, request.Position.AppliesTo, request.Position.Workload, request.Position.Obs));

                await scope.CommitAsync();

                return new ApiContractPersonnelRequest(query);
            }
        }

        [HttpPost("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests/{requestId}/approve")]
        public async Task<ActionResult<ApiContractPersonnelRequest>> ApproveContractorPersonnelRequest([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid requestId)
        {
            var request = await DispatchAsync(new GetContractPersonnelRequest(requestId));

            if (request is null)
                return FusionApiError.NotFound(requestId, "Could not locate request");


            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlExternal();

                r.AnyOf(or =>
                {
                    // The workflow level check will do the logic to see if the user has access in the current workflow state.
                    or.RequestAccess(RequestAccess.Workflow, request);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion


            using (var scope = await BeginTransactionAsync())
            {
                await DispatchAsync(new Logic.Commands.ContractorPersonnelRequest.Approve(request.Id));

                await scope.CommitAsync();
            }

            request = await DispatchAsync(new GetContractPersonnelRequest(requestId));

            return new ApiContractPersonnelRequest(request);
        }

        [HttpPost("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests/{requestId}/reject")]
        public async Task<ActionResult<ApiContractPersonnelRequest>> RejectContractorPersonnelRequest([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid requestId, [FromBody] RejectRequestRequest request)
        {
            if (request == null)
                return await FusionApiError.NoBodyFoundAsync(Request);

            var contractorRequest = await DispatchAsync(new GetContractPersonnelRequest(requestId));

            if (contractorRequest is null)
                return FusionApiError.NotFound(requestId, "Could not locate request");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlExternal();

                r.AnyOf(or =>
                {
                    // The workflow level check will do the logic to see if the user has access in the current workflow state.
                    or.RequestAccess(RequestAccess.Workflow, contractorRequest);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion


            using (var scope = await BeginTransactionAsync())
            {
                await DispatchAsync(new Logic.Commands.ContractorPersonnelRequest.Reject(contractorRequest.Id, request.Reason));

                await scope.CommitAsync();
            }

            contractorRequest = await DispatchAsync(new GetContractPersonnelRequest(requestId));

            return new ApiContractPersonnelRequest(contractorRequest);
        }

        [HttpDelete("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests/{requestId}")]
        public async Task<ActionResult<ApiContractPersonnelRequest>> DeleteContractorRequestById([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid requestId)
        {
            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlExternal();

                r.AnyOf(or =>
                {
                    or.ContractAccess(ContractRole.Any, projectIdentifier, contractIdentifier);
                    or.DelegatedContractAccess(DelegatedContractRole.Any, projectIdentifier, contractIdentifier);
                    or.BeContractorInContract(contractIdentifier);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();


            using (var scope = await BeginTransactionAsync())
            {
                await DispatchAsync(new Logic.Commands.ContractorPersonnelRequest.Delete(requestId));

                await scope.CommitAsync();

                return NoContent();
            }
        }

        [HttpPost("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests/{requestId}/provision")]
        public async Task<ActionResult<ApiContractPersonnelRequest>> ProvisionContractorRequest([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid requestId)
        {
            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AnyOf(or =>
                {
                    or.BeTrustedApplication();
                    or.FullControl();
                    or.FullControlExternal();
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            using (var scope = await BeginTransactionAsync())
            {
                await DispatchAsync(new Logic.Commands.ContractorPersonnelRequest.Provision(requestId));

                await scope.CommitAsync();

                return NoContent();
            }
        }

        #region Comments

        [HttpPost("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests/{requestId}/comments")]
        public async Task<ActionResult> AddRequestComment([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid requestId, [FromBody] RequestCommentRequest create)
        {
            var request = await DispatchAsync(new GetContractPersonnelRequest(requestId));

            if (request == null)
                return FusionApiError.NotFound(requestId, "Request not found");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlExternal();
                r.AnyOf(or =>
                {
                    or.ContractAccess(ContractRole.Any, projectIdentifier, contractIdentifier);
                    or.DelegatedContractAccess(DelegatedContractRole.Any, projectIdentifier, contractIdentifier);
                    or.BeContractorInContract(contractIdentifier);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            await DispatchAsync(new AddComment(User.GetRequestOrigin(), requestId, create.Content));

            return NoContent();
        }

        [HttpPut("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests/{requestId}/comments/{commentId}")]
        public async Task<ActionResult> UpdateRequestComment(
            [FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid requestId, Guid commentId, [FromBody] RequestCommentRequest update)
        {
            var comment = await DispatchAsync(new GetRequestComment(commentId));

            if (comment == null)
                return FusionApiError.NotFound(commentId, "Comment not found");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlExternal();
                r.Must().BeCommentAuthor(comment);

                // Not sure if these are even evaluated, if the comment author req is not successfull.
                // Should be looked at when testing comment functionality.                
                r.AnyOf(or =>
                {
                    or.ContractAccess(ContractRole.Any, projectIdentifier, contractIdentifier);
                    or.DelegatedContractAccess(DelegatedContractRole.Any, projectIdentifier, contractIdentifier);
                    or.BeContractorInContract(contractIdentifier);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            await DispatchAsync(new UpdateComment(commentId, update.Content));

            return NoContent();
        }

        [HttpDelete("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests/{requestId}/comments/{commentId}")]
        public async Task<ActionResult> DeleteRequestComment([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid requestId, Guid commentId)
        {
            var comment = await DispatchAsync(new GetRequestComment(commentId));

            if (comment == null)
                return FusionApiError.NotFound(commentId, "Comment not found");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlExternal();
                r.Must(r => r.BeCommentAuthor(comment));
                r.AnyOf(or =>
                {
                    or.ContractAccess(ContractRole.Any, projectIdentifier, contractIdentifier);
                    or.BeContractorInContract(contractIdentifier);
                });

            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            await DispatchAsync(new DeleteComment(commentId));

            return NoContent();
        }

        #endregion Comments

        #region Options

        [HttpOptions("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests")]
        public async Task<ActionResult> CheckAccessCreateRequests([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid requestId)
        {
            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlExternal();

                r.AnyOf(or =>
                {
                    or.ContractAccess(ContractRole.Any, projectIdentifier, contractIdentifier);
                    or.DelegatedContractAccess(DelegatedContractRole.Any, projectIdentifier, contractIdentifier);
                    or.BeContractorInContract(contractIdentifier);
                });
            });

            if (authResult.Success)
                Response.Headers.Add("Allow", "GET,POST");
            else
                Response.Headers.Add("Allow", "GET");

            return NoContent();
        }


        [HttpOptions("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests/{requestId}")]
        public async Task<ActionResult> CheckAccessUpdateRequest([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid requestId)
        {
            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlExternal();

                r.AnyOf(or =>
                {
                    or.ContractAccess(ContractRole.Any, projectIdentifier, contractIdentifier);
                    or.DelegatedContractAccess(DelegatedContractRole.Any, projectIdentifier, contractIdentifier);
                });
            });

            if (authResult.Success)
                Response.Headers.Add("Allow", "GET,PUT,DELETE");
            else
                Response.Headers.Add("Allow", "GET");


            return NoContent();
        }

        [HttpOptions("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests/{requestId}/actions/{actionName}")]
        public async Task<ActionResult> CheckAccessRequestAction([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid requestId, string actionName)
        {
            var request = await DispatchAsync(new GetContractPersonnelRequest(requestId));

            if (request is null)
                return FusionApiError.NotFound(requestId, "Could not locate request");

            #region "Comment"

            if (actionName.ToLower() == "comment")
            {
                var commentAuthResult = await Request.RequireAuthorizationAsync(r =>
                {
                    r.AlwaysAccessWhen().FullControl().FullControlExternal();

                    r.AnyOf(or =>
                    {
                        or.ContractAccess(ContractRole.Any, projectIdentifier, contractIdentifier);
                        or.DelegatedContractAccess(DelegatedContractRole.Any, projectIdentifier, contractIdentifier);
                        or.BeContractorInContract(contractIdentifier);
                    });
                });

                if (commentAuthResult.Success)
                    Response.Headers.Add("Allow", "POST");
                else
                    Response.Headers.Add("Allow", "");

                return NoContent();
            }

            #endregion

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlExternal();

                r.AnyOf(or =>
                {
                    // The workflow level check will do the logic to see if the user has access in the current workflow state.
                    or.RequestAccess(RequestAccess.Workflow, request);
                });
            });

            if (authResult.Success)
                Response.Headers.Add("Allow", "POST");
            else
                Response.Headers.Add("Allow", "");

            return NoContent();
        }

        #endregion

    }
}
