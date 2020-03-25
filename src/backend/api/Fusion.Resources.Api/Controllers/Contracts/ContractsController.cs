using Bogus;
using Fusion.Integration.Profile;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Transactions;
using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Authorization;
using Fusion.Resources.Api.Authorization;

namespace Fusion.Resources.Api.Controllers
{
    [Authorize]
    [ApiController]
    public class ContractsController : ResourceControllerBase
    {
        private readonly IMediator mediator;
        private readonly IOrgApiClientFactory orgApiClientFactory;

        public ContractsController(IMediator mediator, IOrgApiClientFactory orgApiClientFactory)
        {
            this.mediator = mediator;
            this.orgApiClientFactory = orgApiClientFactory;
        }

        [HttpGet("/projects/{projectIdentifier}/contracts")]
        public async Task<ActionResult<ApiCollection<ApiContract>>> GetProjectAllocatedContract([FromRoute]ProjectIdentifier projectIdentifier)
        {
            // Not sure if there is any restrictions on listing contracts for a project.

            var client = orgApiClientFactory.CreateClient(ApiClientMode.Application);
            var realContracts = await client.GetContractsV2Async(projectIdentifier.ProjectId);

            var allocatedContracts = await DispatchAsync(GetProjectContracts.ByOrgProjectId(projectIdentifier.ProjectId));

            var contractsToReturn = realContracts
                .Where(c => allocatedContracts.Any(ac => ac.OrgContractId == c.Id))
                .ToList();


            // Trim contracts
            switch (User.GetUserAccountType())
            {
                case FusionAccountType.External:
                    contractsToReturn.RemoveAll(c => User.IsInContract(c.ContractNumber) == false);
                    break;
            } 

            var collection = new ApiCollection<ApiContract>(contractsToReturn.Select(c => new ApiContract(c)));
            return collection;
        }

        [HttpGet("/projects/{projectIdentifier}/contracts/{contractId}")]
        public async Task<ActionResult<ApiContract>> GetProjectContract([FromRoute]ProjectIdentifier projectIdentifier, Guid contractId)
        {
            var client = orgApiClientFactory.CreateClient(ApiClientMode.Application);
            var orgContract = await client.GetContractV2Async(projectIdentifier.ProjectId, contractId);

            return new ApiContract(orgContract);
        }

        [HttpGet("/projects/{projectIdentifier}/available-contracts")]
        public async Task<ActionResult<ApiCollection<ApiUnallocatedContract>>> GetProjectAvailableContracts([FromRoute]ProjectIdentifier projectIdentifier)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();

                r.AnyOf(or =>
                {
                    or.ProjectAccess(ProjectAccess.ManageContracts, projectIdentifier);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion


            await Task.Delay(1);

            var contracts = new[]
            {
                new ApiUnallocatedContract { ContractNumber = "0000000001" },
                new ApiUnallocatedContract { ContractNumber = "0000000002" },
                new ApiUnallocatedContract { ContractNumber = "0000000003" },
                new ApiUnallocatedContract { ContractNumber = "0000000004" },
                new ApiUnallocatedContract { ContractNumber = "0000055555" },
                new ApiUnallocatedContract { ContractNumber = "0000666666" },
                new ApiUnallocatedContract { ContractNumber = "1000000000" },
                new ApiUnallocatedContract { ContractNumber = "1111111111" }
            };

            return Ok(new ApiCollection<ApiUnallocatedContract>(contracts));
        }

        [HttpPost("/projects/{projectIdentifier}/contracts")]
        public async Task<ActionResult<ApiContract>> AllocateProjectContract([FromRoute]ProjectIdentifier projectIdentifier, [FromBody] ContractRequest request)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();

                r.AnyOf(or =>
                {
                    or.ProjectAccess(ProjectAccess.ManageContracts, projectIdentifier);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var allocatedContract = await mediator.Send(new AllocateContract(projectIdentifier.ProjectId, request.ContractNumber));
            allocatedContract = await mediator.Send(new UpdateContract(projectIdentifier.ProjectId, allocatedContract.OrgContractId)
            {
                Name = request.Name,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                CompanyId = request.Company?.Id,
                Description = request.Description
            });

            await DispatchAsync(new UpdateContractReps(projectIdentifier.ProjectId, allocatedContract.OrgContractId)
            {
                CompanyRepPositionId = request.CompanyRepPositionId,
                ContractResponsiblePositionId = request.ContractResponsiblePositionId
            });

            await DispatchAsync(new UpdateContractExternalReps(projectIdentifier.ProjectId, allocatedContract.OrgContractId)
            {
                CompanyRepPositionId = request.ExternalCompanyRepPositionId,
                ContractResponsiblePositionId = request.ExternalContractResponsiblePositionId
            });

            var client = orgApiClientFactory.CreateClient(ApiClientMode.Application);
            var orgContract = await client.GetContractV2Async(projectIdentifier.ProjectId, allocatedContract.OrgContractId);

            return Created($"/projects/{projectIdentifier}/contracts/{request.ContractNumber}", new ApiContract(orgContract));
        }

        [HttpPut("/projects/{projectIdentifier}/contracts/{contractIdentifier}")]
        public async Task<ActionResult<ApiContract>> UpdateProjectContract([FromRoute]ProjectIdentifier projectIdentifier, Guid contractIdentifier, [FromBody] ContractRequest request)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();

                r.AnyOf(or =>
                {
                    or.ProjectAccess(ProjectAccess.ManageContracts, projectIdentifier);
                    or.ContractAccess(ContractRole.AnyInternalRole, projectIdentifier, contractIdentifier);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            await DispatchAsync(new UpdateContract(projectIdentifier.ProjectId, contractIdentifier)
            {
                Name = request.Name,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                CompanyId = request.Company?.Id,
                Description = request.Description
            });

            await DispatchAsync(new UpdateContractReps(projectIdentifier.ProjectId, contractIdentifier)
            {
                CompanyRepPositionId = request.CompanyRepPositionId,
                ContractResponsiblePositionId = request.ContractResponsiblePositionId
            });

            await DispatchAsync(new UpdateContractExternalReps(projectIdentifier.ProjectId, contractIdentifier)
            {
                CompanyRepPositionId = request.ExternalCompanyRepPositionId,
                ContractResponsiblePositionId = request.ExternalContractResponsiblePositionId
            });

           
            var client = orgApiClientFactory.CreateClient(ApiClientMode.Application);
            var orgContract = await client.GetContractV2Async(projectIdentifier.ProjectId, contractIdentifier);

            return Created($"/projects/{projectIdentifier}/contracts/{request.ContractNumber}", new ApiContract(orgContract));
        }

        [HttpPut("/projects/{projectIdentifier}/contracts/{contractIdentifier}/external-company-representative")]
        public async Task<ActionResult<ApiClients.Org.ApiPositionV2>> EnsureContractExternalCompanyRep([FromRoute]ProjectIdentifier projectIdentifier, Guid contractIdentifier, [FromBody] ContractPositionRequest request)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();

                r.AnyOf(or =>
                {
                    or.ProjectAccess(ProjectAccess.ManageContracts, projectIdentifier);
                    or.ContractAccess(ContractRole.AnyInternalRole, projectIdentifier, contractIdentifier);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var externalId = "ext-comp-rep";
            var existingPosition = await DispatchAsync(GetContractPosition.ByExternalId(projectIdentifier.ProjectId, contractIdentifier, externalId));

            ApiClients.Org.ApiPositionV2 position;

            if (existingPosition != null)
            {
                position = await DispatchAsync(new UpdateContractPosition(existingPosition)
                {
                    BasePositionId = request.BasePosition.Id,
                    PositionName = request.Name,
                    AppliesFrom = request.AppliesFrom,
                    AppliesTo = request.AppliesTo,
                    Workload = request.Workload,
                    Obs = request.Obs,
                    AssignedPerson = request.AssignedPerson
                });
            }
            else
            {
                var createNewPositionCommand = new CreateContractPosition(projectIdentifier.ProjectId, contractIdentifier)
                {
                    BasePositionId = request.BasePosition.Id,
                    PositionName = request.Name,
                    AppliesFrom = request.AppliesFrom,
                    AppliesTo = request.AppliesTo,
                    Workload = request.Workload,
                    Obs = request.Obs,
                    AssignedPerson = request.AssignedPerson,
                    ExternalId = externalId
                };

                position = await DispatchAsync(createNewPositionCommand);
            }
            
            await DispatchAsync(new UpdateContractExternalReps(projectIdentifier.ProjectId, contractIdentifier) { CompanyRepPositionId = position.Id });

            return position;
        }

        [HttpPut("/projects/{projectIdentifier}/contracts/{contractIdentifier}/external-contract-responsible")]
        public async Task<ActionResult<ApiClients.Org.ApiPositionV2>> EnsureContractExternalContractResp([FromRoute]ProjectIdentifier projectIdentifier, Guid contractIdentifier, [FromBody] ContractPositionRequest request)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();

                r.AnyOf(or =>
                {
                    or.ProjectAccess(ProjectAccess.ManageContracts, projectIdentifier);
                    or.ContractAccess(ContractRole.AnyInternalRole, projectIdentifier, contractIdentifier);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var externalId = "ext-contr-resp";
            var existingPosition = await DispatchAsync(GetContractPosition.ByExternalId(projectIdentifier.ProjectId, contractIdentifier, externalId));

            ApiClients.Org.ApiPositionV2 position;

            if (existingPosition != null)
            {
                position = await DispatchAsync(new UpdateContractPosition(existingPosition)
                {
                    BasePositionId = request.BasePosition.Id,
                    PositionName = request.Name,
                    AppliesFrom = request.AppliesFrom,
                    AppliesTo = request.AppliesTo,
                    Workload = request.Workload,
                    AssignedPerson = request.AssignedPerson
                });
            }
            else
            {
                var createNewPositionCommand = new CreateContractPosition(projectIdentifier.ProjectId, contractIdentifier)
                {
                    BasePositionId = request.BasePosition.Id,
                    PositionName = request.Name,
                    AppliesFrom = request.AppliesFrom,
                    AppliesTo = request.AppliesTo,
                    Workload = request.Workload,
                    AssignedPerson = request.AssignedPerson,
                    ExternalId = externalId
                };

                position = await DispatchAsync(createNewPositionCommand);
            }

            await DispatchAsync(new UpdateContractExternalReps(projectIdentifier.ProjectId, contractIdentifier) { CompanyRepPositionId = position.Id });

            return position;
        }
    }
}
