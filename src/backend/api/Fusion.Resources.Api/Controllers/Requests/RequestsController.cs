using Bogus;
using Fusion.Integration.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fusion.Integration;
using Fusion.Resources.Domain.Queries;
using Fusion.Resources.Domain;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using Fusion.Resources.Domain.Commands;
using Fusion.AspNetCore.OData;
using Fusion.Resources.Api.Middleware;
using Microsoft.AspNetCore.Http;

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
        /// <returns></returns>
        [HttpGet("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests")]
        public async Task<ActionResult<ApiCollection<ApiContractPersonnelRequest>>> GetContractRequests([FromRoute]ProjectIdentifier projectIdentifier, Guid contractIdentifier, [FromQuery]ODataQueryParams query)
        {
            var requests = await DispatchAsync(GetContractPersonnelRequests.QueryContract(projectIdentifier.ProjectId, contractIdentifier).WithQuery(query));

            return new ApiCollection<ApiContractPersonnelRequest>(requests.Select(r => new ApiContractPersonnelRequest(r)));
        }

        [HttpGet("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests/{requestId}")]
        public async Task<ActionResult<ApiContractPersonnelRequest>> GetContractRequestById([FromRoute]ProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid requestId)
        {
            var request = await DispatchAsync(new GetContractPersonnelRequest(requestId));
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
        public async Task<ActionResult<ApiContractPersonnelRequest>> CreatePersonnelRequest([FromRoute]ProjectIdentifier projectIdentifier, Guid contractIdentifier, [FromBody] ContractPersonnelRequestRequest request)
        {
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
        public async Task<ActionResult<ApiContractPersonnelRequest>> UpdatePersonnelRequest([FromRoute]ProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid requestId, [FromBody] ContractPersonnelRequestRequest request)
        {
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
        public async Task<ActionResult<ApiContractPersonnelRequest>> ApproveContractorPersonnelRequest([FromRoute]ProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid requestId)
        {
            var request = await DispatchAsync(new GetContractPersonnelRequest(requestId));

            if (request is null)
                return FusionApiError.NotFound(requestId, "Could not locate request");


            using (var scope = await BeginTransactionAsync())
            {
                await DispatchAsync(new Logic.Commands.ContractorPersonnelRequest.Approve(request.Id));
                
                await scope.CommitAsync();
            }

            request = await DispatchAsync(new GetContractPersonnelRequest(requestId));

            return new ApiContractPersonnelRequest(request);
        }

        [HttpPost("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests/{requestId}/reject")]
        public async Task<ActionResult<ApiContractPersonnelRequest>> RejectContractorPersonnelRequest([FromRoute]ProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid requestId, [FromBody] RejectRequestRequest request)
        {
            if (request == null)
                return await FusionApiError.NoBodyFoundAsync(Request);

            var contractorRequest = await DispatchAsync(new GetContractPersonnelRequest(requestId));

            if (contractorRequest is null)
                return FusionApiError.NotFound(requestId, "Could not locate request");


            using (var scope = await BeginTransactionAsync())
            {
                await DispatchAsync(new Logic.Commands.ContractorPersonnelRequest.Reject(contractorRequest.Id, request.Reason));

                await scope.CommitAsync();
            }

            contractorRequest = await DispatchAsync(new GetContractPersonnelRequest(requestId));

            return new ApiContractPersonnelRequest(contractorRequest);
        }


        [HttpDelete("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests/{requestId}")]
        public async Task<ActionResult<ApiContractPersonnelRequest>> DeleteContractorRequestById([FromRoute]ProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid requestId)
        {
            using (var scope = await BeginTransactionAsync())
            {
                await DispatchAsync(new Logic.Commands.ContractorPersonnelRequest.Delete(requestId));

                await scope.CommitAsync();

                return NoContent();
            }
        }

        [HttpPost("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests/{requestId}/provision")]
        public async Task<ActionResult<ApiContractPersonnelRequest>> ProvisionContractorRequest([FromRoute]ProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid requestId)
        {
            using (var scope = await BeginTransactionAsync())
            {
                await DispatchAsync(new Logic.Commands.ContractorPersonnelRequest.Provision(requestId));

                await scope.CommitAsync();

                return NoContent();
            }
        }


        #region Options
        [HttpOptions("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests")]
        public async Task<ActionResult> CheckAccessCreateRequests(string projectIdentifier, string contractIdentifier, Guid requestId, string actionName)
        {
            var faker = new Faker();

            if (faker.Random.Bool())
                Response.Headers.Add("Allow", "GET,POST");
            else
                Response.Headers.Add("Allow", "GET");

            return NoContent();
        }


        [HttpOptions("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests/{requestId}")]
        public async Task<ActionResult> CheckAccessUpdateRequest(string projectIdentifier, string contractIdentifier, Guid requestId, string actionName)
        {
            var faker = new Faker();

            if (faker.Random.Bool())
                Response.Headers.Add("Allow", "GET,PUT,DELETE");
            else
                Response.Headers.Add("Allow", "GET");

            return NoContent();
        }

        [HttpOptions("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests/{requestId}/actions/{actionName}")]
        public async Task<ActionResult> CheckAccessRequestAction(string projectIdentifier, string contractIdentifier, Guid requestId, string actionName)
        {
            var faker = new Faker();

            if (faker.Random.Bool())
                Response.Headers.Add("Allow", "POST");
            else
                Response.Headers.Add("Allow", "");

            return NoContent();
        }
        #endregion


        
    }


}
