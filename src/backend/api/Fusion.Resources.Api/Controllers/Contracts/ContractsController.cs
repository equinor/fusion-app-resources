using Fusion.Integration;
using Fusion.Resources.Api.Configuration;
using Fusion.Integration.Profile;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Authorization;
using Fusion.Resources.Api.Authorization;
using Fusion.Integration.Org;
using System.Threading;
using FluentValidation;
using System.Collections.Generic;
using ContractRole = Fusion.Resources.Api.Authorization.ContractRole;

namespace Fusion.Resources.Api.Controllers
{
    [Authorize]
    [ApiController]
    public class ContractsController : ResourceControllerBase
    {
        private readonly IMediator mediator;
        private readonly IProjectOrgResolver orgResolver;

        public ContractsController(IMediator mediator, IProjectOrgResolver orgResolver)
        {
            this.mediator = mediator;
            this.orgResolver = orgResolver;
        }

        [HttpGet("/projects/{projectIdentifier}/contracts")]
        public async Task<ActionResult<ApiCollection<ApiContract>>> GetProjectAllocatedContract([FromRoute]ApiProjectIdentifier projectIdentifier)
        {
            var delegatedAccess = await DispatchAsync(Domain.GetContractDelegatedRoles.ForProject(projectIdentifier.ProjectId));
            var limitedContractAccess = delegatedAccess.Select(r => r.Contract.OrgContractId).ToList();
            var hasLimitedAccess = limitedContractAccess.Any();

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();

                r.AnyOf(or =>
                {
                    or.BeTrustedApplication();
                    or.BeEmployee();
                    or.BeContractorInProject(projectIdentifier);
                    or.HaveOrgchartPosition(ProjectOrganisationIdentifier.FromOrgChartId(projectIdentifier.ProjectId));
                });
            });

            if (authResult.Unauthorized)
            {

                if (!hasLimitedAccess)
                    return authResult.CreateForbiddenResponse();
            }

            #endregion

            var allocatedContracts = await DispatchAsync(GetProjectContracts.ByOrgProjectId(projectIdentifier.ProjectId));

            var pager = new SemaphoreSlim(10);
            var orgContracts = await Task.WhenAll(allocatedContracts.Select(async c =>
            {
                await pager.WaitAsync();
                try { return await orgResolver.ResolveContractAsync(projectIdentifier.ProjectId, c.OrgContractId); }
                finally { pager.Release(); }
            }));

            var contractsToReturn = orgContracts
                .Where(c => c != null)
                .Where(c => allocatedContracts.Any(ac => ac.OrgContractId == c!.Id))
                .Select(c => c!)
                .ToList();

            // Trim contracts
            switch (User.GetUserAccountType())
            {
                case FusionAccountType.External:
                    var relevantContracts = contractsToReturn.Where(c => User.IsInContract(c!.ContractNumber) || limitedContractAccess.Contains(c!.Id));
                    contractsToReturn.RemoveAll(c => !relevantContracts.Any(rc => rc!.Id == c!.Id));
                    break;
            } 

            var collection = new ApiCollection<ApiContract>(contractsToReturn.Select(c => new ApiContract(c!)));
            return collection;
        }

        [HttpGet("/projects/{projectIdentifier}/contracts/{contractId}")]
        public async Task<ActionResult<ApiContract>> GetProjectContract([FromRoute]ApiProjectIdentifier projectIdentifier, Guid contractId)
        {
            // Not sure if there is any restrictions on listing contracts for a project.
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();

                r.AnyOf(or =>
                {
                    or.BeEmployee();
                    or.BeContractorInContract(contractId);
                    or.ContractAccess(ContractRole.AnyExternalRole, projectIdentifier, contractId);
                    or.DelegatedContractAccess(DelegatedContractRole.Any, projectIdentifier, contractId);
                    or.HaveOrgchartPosition(ProjectOrganisationIdentifier.FromOrgChartId(projectIdentifier.ProjectId));
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var orgContract = await orgResolver.ResolveContractAsync(projectIdentifier.ProjectId, contractId);

            if (orgContract is null)
                return ApiErrors.NotFound("Could not locate contract", $"/projects/{projectIdentifier.OriginalIdentifier}/contracts/{contractId}");


            return new ApiContract(orgContract);
        }

        [HttpGet("/projects/{projectIdentifier}/available-contracts")]
        public async Task<ActionResult<ApiCollection<ApiUnallocatedContract>>> GetProjectAvailableContracts(
            [FromRoute]ApiProjectIdentifier projectIdentifier,
            [FromServices] IHttpClientFactory httpClientFactory,
            [FromServices] IFusionContextResolver contextResolver)
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

            FusionContext projectMasterContext;

            try
            {
                projectMasterContext = await contextResolver.ResolveProjectMasterAsync(projectIdentifier);
            }
            catch (ContextResolverExtensions.ProjectMasterNotFoundError ex)
            {
                return ApiErrors.InvalidOperation(ex);
            }

            var commonlibClient = httpClientFactory.CreateClient(HttpClientNames.AppCommonLib);
            var response = await commonlibClient.GetAsync($"/projects/{projectMasterContext.ExternalId}/contracts");

            if (!response.IsSuccessStatusCode)
                return ApiErrors.FailedFusionRequest(FusionEndpoint.CommonLib, "Failed to get contracts for project");

            var body = await response.Content.ReadAsStringAsync();
            var items = JsonConvert.DeserializeAnonymousType(body, new[] { new { Name = string.Empty, ContractNumber = string.Empty, CompanyName = string.Empty } });
            var allocatedContracts = await DispatchAsync(GetProjectContracts.ByOrgProjectId(projectIdentifier.ProjectId));

            var list = items
                .Where(item => !allocatedContracts.Any(ac => ac.ContractNumber == item.ContractNumber))
                .Select(item => new ApiUnallocatedContract
                {
                    Name = item.Name,
                    ContractNumber = item.ContractNumber,
                    CompanyName = item.CompanyName
                });

            return Ok(new ApiCollection<ApiUnallocatedContract>(list));
        }

        [HttpPost("/projects/{projectIdentifier}/contracts")]
        public async Task<ActionResult<ApiContract>> AllocateProjectContract([FromRoute]ApiProjectIdentifier projectIdentifier, [FromBody] ContractRequest request)
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

            var orgContract = await orgResolver.ResolveContractAsync(projectIdentifier.ProjectId, allocatedContract.OrgContractId);

            return Created($"/projects/{projectIdentifier}/contracts/{request.ContractNumber}", new ApiContract(orgContract!));
        }

        [HttpPut("/projects/{projectIdentifier}/contracts/{contractIdentifier}")]
        public async Task<ActionResult<ApiContract>> UpdateProjectContract([FromRoute]ApiProjectIdentifier projectIdentifier, Guid contractIdentifier, [FromBody] ContractRequest request)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();

                r.AnyOf(or =>
                {
                    or.ProjectAccess(ProjectAccess.ManageContracts, projectIdentifier);
                    or.ContractAccess(ContractRole.AnyInternalRole, projectIdentifier, contractIdentifier);
                    or.DelegatedContractAccess(DelegatedContractRole.AnyInternalRole, projectIdentifier, contractIdentifier);
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

            var orgContract = await orgResolver.ResolveContractAsync(projectIdentifier.ProjectId, contractIdentifier);

            return Created($"/projects/{projectIdentifier}/contracts/{request.ContractNumber}", new ApiContract(orgContract!));
        }

        [HttpPut("/projects/{projectIdentifier}/contracts/{contractIdentifier}/external-company-representative")]
        public async Task<ActionResult<ApiClients.Org.ApiPositionV2>> EnsureContractExternalCompanyRep([FromRoute]ApiProjectIdentifier projectIdentifier, Guid contractIdentifier, [FromBody] ContractPositionRequest request)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();

                r.AnyOf(or =>
                {
                    or.ProjectAccess(ProjectAccess.ManageContracts, projectIdentifier);
                    or.ContractAccess(ContractRole.AnyInternalRole, projectIdentifier, contractIdentifier);
                    or.DelegatedContractAccess(DelegatedContractRole.AnyInternalRole, projectIdentifier, contractIdentifier);
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
        public async Task<ActionResult<ApiClients.Org.ApiPositionV2>> EnsureContractExternalContractResp([FromRoute]ApiProjectIdentifier projectIdentifier, Guid contractIdentifier, [FromBody] ContractPositionRequest request)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();

                r.AnyOf(or =>
                {
                    or.ProjectAccess(ProjectAccess.ManageContracts, projectIdentifier);
                    or.ContractAccess(ContractRole.AnyInternalRole, projectIdentifier, contractIdentifier);
                    or.DelegatedContractAccess(DelegatedContractRole.AnyInternalRole, projectIdentifier, contractIdentifier);
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

            await DispatchAsync(new UpdateContractExternalReps(projectIdentifier.ProjectId, contractIdentifier) { ContractResponsiblePositionId = position.Id });

            return position;
        }

        #region Role delegation


        [HttpGet("/projects/{projectIdentifier}/contracts/{contractIdentifier}/delegated-roles")]
        public async Task<ActionResult<List<ApiDelegatedRole>>> GetContractDelegatedRoles([FromRoute] ApiProjectIdentifier projectIdentifier, Guid contractIdentifier)
        {
            // Not sure if there is any restrictions on listing contracts for a project.
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();

                r.AnyOf(or =>
                {
                    or.BeEmployee();
                    or.BeContractorInContract(contractIdentifier);
                    or.ContractAccess(ContractRole.AnyExternalRole, projectIdentifier, contractIdentifier);
                    or.DelegatedContractAccess(DelegatedContractRole.Any, projectIdentifier, contractIdentifier);
                    or.HaveOrgchartPosition(ProjectOrganisationIdentifier.FromOrgChartId(projectIdentifier.ProjectId));
                    or.BeTrustedApplication();
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var delegatedRoles = await DispatchAsync(Domain.GetContractDelegatedRoles.ForContract(projectIdentifier.ProjectId, contractIdentifier));
            return delegatedRoles.Select(r => new ApiDelegatedRole(r)).ToList();
        }

        [HttpPost("/projects/{projectIdentifier}/contracts/{contractIdentifier}/delegated-roles")]
        public async Task<ActionResult<ApiDelegatedRole>> CreateContractDelegatedRole([FromRoute] ApiProjectIdentifier projectIdentifier, Guid contractIdentifier, [FromBody] CreateDelegatedRoleRequest request)
        {

            #region Authorization

            // Must handle the different classifications differently

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();

                r.AnyOf(or =>
                {
                    switch (request.Classification)
                    {
                        case ApiDelegatedRoleClassification.Internal:
                            or.CanDelegateInternalRole(projectIdentifier, contractIdentifier);
                            break;

                        case ApiDelegatedRoleClassification.External:
                            or.CanDelegateExternalRole(projectIdentifier, contractIdentifier);
                            break;
                    }
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion


            try
            {
                using (var scope = await BeginTransactionAsync())
                {
                    var command = new Domain.Commands.CreateRoleDelegation(projectIdentifier.ProjectId, contractIdentifier)
                        .ValidToDate(request.ValidTo)
                        .ForPerson(request.Person)
                        .SetIsInternal(request.Classification == ApiDelegatedRoleClassification.Internal);

                    var newRole = await DispatchAsync(command);

                    await scope.CommitAsync();

                    return new ApiDelegatedRole(newRole);
                }
            }
            catch (InvalidOperationException ex)
            {
                return ApiErrors.InvalidOperation(ex);
            }
            catch (ValidationException ex)
            {
                return ApiErrors.InvalidOperation(ex);
            }
            catch (RoleDelegationExistsError ex)
            {
                return ApiErrors.InvalidOperation(ex);
            }
        }

        [HttpDelete("/projects/{projectIdentifier}/contracts/{contractIdentifier}/delegated-roles/{roleId}")]
        public async Task<ActionResult> DeleteContractDelegatedRole([FromRoute] ApiProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid roleId)
        {
            var role = await DispatchAsync(new GetContractDelegatedRole(roleId));

            if (role == null)
                return ApiErrors.NotFound("Could not locate role", $"{roleId}");
            if (role.Project.OrgProjectId != projectIdentifier.ProjectId || role.Contract.OrgContractId != contractIdentifier)
                return ApiErrors.NotFound("Could not locate role", $"/projects/{projectIdentifier.OriginalIdentifier}/contracts/{contractIdentifier}/delegated-roles/{roleId}");


            #region Authorization

            // Must handle the different classifications differently

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();

                r.AnyOf(or =>
                {
                    switch (role.Classification)
                    {
                        case Database.Entities.DbDelegatedRoleClassification.Internal:
                            or.CanDelegateInternalRole(projectIdentifier, contractIdentifier);
                            break;

                        case Database.Entities.DbDelegatedRoleClassification.External:
                            or.CanDelegateExternalRole(projectIdentifier, contractIdentifier);
                            break;
                    }
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion



            try
            {
                using (var scope = await BeginTransactionAsync())
                {
                    var command = new Domain.Commands.DeleteRoleDelegation(roleId);
                    await DispatchAsync(command);

                    await scope.CommitAsync();
                }
            }
            catch (InvalidOperationException ex)
            {
                return ApiErrors.InvalidOperation(ex);
            }
            catch (ValidationException ex)
            {
                return ApiErrors.InvalidOperation(ex);
            }
            return Ok();
        }

        [HttpPatch("/projects/{projectIdentifier}/contracts/{contractIdentifier}/delegated-roles/{roleId}")]
        public async Task<ActionResult<ApiDelegatedRole>> UpdateContractDelegatedRole([FromRoute] ApiProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid roleId, [FromBody] PatchDelegatedRoleRequest request)
        {
            var role = await DispatchAsync(new GetContractDelegatedRole(roleId));

            if (role == null)
                return ApiErrors.NotFound("Could not locate role", $"{roleId}");
            if (role.Project.OrgProjectId != projectIdentifier.ProjectId || role.Contract.OrgContractId != contractIdentifier)
                return ApiErrors.NotFound("Could not locate role", $"/projects/{projectIdentifier.OriginalIdentifier}/contracts/{contractIdentifier}/delegated-roles/{roleId}");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();

                r.AnyOf(or =>
                {
                    switch (role.Classification)
                    {
                        case Database.Entities.DbDelegatedRoleClassification.Internal:
                            or.CanDelegateInternalRole(projectIdentifier, contractIdentifier);
                            break;

                        case Database.Entities.DbDelegatedRoleClassification.External:
                            or.CanDelegateExternalRole(projectIdentifier, contractIdentifier);
                            break;
                    }
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            using (var scope = await BeginTransactionAsync())
            {
                if (request.ValidTo.HasValue)
                {
                    var command = new Domain.Commands.RecertifyRoleDelegation(roleId, request.ValidTo.Value);
                    role = await DispatchAsync(command);
                }

                await scope.CommitAsync();
            }

            return new ApiDelegatedRole(role);
        }

        [HttpOptions("/projects/{projectIdentifier}/contracts/{contractIdentifier}/delegated-roles")]
        public async Task<ActionResult> CheckContractDelegationAccess([FromRoute] ApiProjectIdentifier projectIdentifier, Guid contractIdentifier, [FromQuery] string classification)
        {
            if (!Enum.TryParse(classification, true, out ApiDelegatedRoleClassification roleClassification))
            {
                return FusionApiError.InvalidOperation("InvalidArgument", $"Invalid classification, allowed values: '{ApiDelegatedRoleClassification.Internal}', '{ApiDelegatedRoleClassification.External}'");
            }

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();

                r.AnyOf(or =>
                {
                    switch (roleClassification)
                    {
                        case ApiDelegatedRoleClassification.Internal:
                            or.CanDelegateInternalRole(projectIdentifier, contractIdentifier);
                            break;

                        case ApiDelegatedRoleClassification.External:
                            or.CanDelegateExternalRole(projectIdentifier, contractIdentifier);
                            break;
                    }
                });
            });

            if (authResult.Success)
                Response.Headers.Add("Allow", "GET,POST,DELETE");
            else
                Response.Headers.Add("Allow", "GET");

            return NoContent();
        }


        #endregion
    }
}
