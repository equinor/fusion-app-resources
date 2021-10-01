using FluentValidation;
using Fusion.AspNetCore.Api;
using Fusion.AspNetCore.FluentAuthorization;
using Fusion.AspNetCore.OData;
using Fusion.Authorization;
using Fusion.Integration;
using Fusion.Integration.Org;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands;
using Fusion.Resources.Domain.Queries;
using Fusion.Resources.Logic;
using Fusion.Resources.Logic.Requests;
using Fusion.Resources.Logic.Workflows;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Fusion.Resources.Domain.Notifications.InternalRequests;

namespace Fusion.Resources.Api.Controllers
{
    [ApiVersion("1.0-preview")]
    [ApiVersion("1.0")]
    [Authorize]
    [ApiController]
    public class InternalRequestsController : ResourceControllerBase
    {
        private readonly IFusionProfileResolver profileResolver;

        public InternalRequestsController(IFusionProfileResolver profileResolver)
        {
            this.profileResolver = profileResolver;
        }

        [HttpPost("/projects/{projectIdentifier}/resources/requests")]
        [HttpPost("/projects/{projectIdentifier}/requests")]
        public async Task<ActionResult<ApiResourceAllocationRequest>> CreateProjectAllocationRequest(
            [FromRoute] PathProjectIdentifier projectIdentifier, [FromBody] CreateResourceAllocationRequest request)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    or.OrgChartPositionWriteAccess(projectIdentifier.ProjectId, request.OrgPositionId);
                });
            });


            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            if (request.ResolveType() == InternalRequestType.Allocation)
            {
                // Must resolve the subType to use when allocation request.
                if (string.IsNullOrEmpty(request.SubType))
                    request.SubType = await DispatchAsync(new Logic.Commands.ResourceAllocationRequest.ResolveSubType(request.OrgPositionId, request.OrgPositionInstanceId));
            }
            // Create all requests as draft
            var command = new CreateInternalRequest(InternalRequestOwner.Project, request.ResolveType())
            {
                SubType = request.SubType,
                AdditionalNote = request.AdditionalNote,
                OrgPositionId = request.OrgPositionId,
                OrgProjectId = projectIdentifier.ProjectId,
                OrgPositionInstanceId = request.OrgPositionInstanceId,
                AssignedDepartment = request.AssignedDepartment,
                ProposedPersonAzureUniqueId = request.ProposedPersonAzureUniqueId,
            };

            try
            {
                using var transaction = await BeginTransactionAsync();

                var newRequest = await DispatchAsync(command);

                if (request.ProposedChanges is not null || request.ProposedPersonAzureUniqueId is not null)
                {
                    newRequest = await DispatchAsync(new UpdateInternalRequest(newRequest.RequestId)
                    {
                        ProposedChanges = request.ProposedChanges,
                        ProposedPersonAzureUniqueId = request.ProposedPersonAzureUniqueId
                    });
                }

                await transaction.CommitAsync();

                newRequest = await DispatchAsync(new GetResourceAllocationRequestItem(newRequest.RequestId).ExpandAll());

                return Created($"/projects/{projectIdentifier}/requests/{newRequest!.RequestId}", new ApiResourceAllocationRequest(newRequest));
            }
            catch (ValidationException ex)
            {
                return ApiErrors.InvalidOperation(ex);
            }

        }

        [HttpPost("/projects/{projectIdentifier}/resources/requests/$batch")]
        [HttpPost("/projects/{projectIdentifier}/requests/$batch")]
        public async Task<ActionResult<ApiResourceAllocationRequest>> CreateProjectAllocationRequestV2(
           [FromRoute] PathProjectIdentifier projectIdentifier, [FromBody] BatchCreateResourceAllocationRequest request)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    or.OrgChartPositionWriteAccess(projectIdentifier.ProjectId, request.OrgPositionId);
                });
            });


            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            if (request.ResolveType() == InternalRequestType.Allocation)
            {
                // Must resolve the subType to use when allocation request.
                if (string.IsNullOrEmpty(request.SubType))
                {
                    request.SubType = await DispatchAsync(new Logic.Commands.ResourceAllocationRequest.ResolveSubType(request.OrgPositionId, request.OrgPositionInstanceIds.First()));
                }
            }

            var requests = new List<QueryResourceAllocationRequest>();
            var correlationId = Guid.NewGuid();
            try
            {
                using var transaction = await BeginTransactionAsync();
                foreach (var instanceId in request.OrgPositionInstanceIds)
                {
                    // Create all requests as draft
                    var command = new CreateInternalRequest(InternalRequestOwner.Project, request.ResolveType())
                    {
                        SubType = request.SubType,
                        AdditionalNote = request.AdditionalNote,
                        OrgPositionId = request.OrgPositionId,
                        OrgProjectId = projectIdentifier.ProjectId,
                        OrgPositionInstanceId = instanceId,
                        AssignedDepartment = request.AssignedDepartment,
                        ProposedPersonAzureUniqueId = request.ProposedPersonAzureUniqueId,
                        CorrelationId = correlationId
                    };

                    var newRequest = await DispatchAsync(command);

                    if (request.ProposedChanges is not null || request.ProposedPersonAzureUniqueId is not null)
                    {
                        newRequest = await DispatchAsync(new UpdateInternalRequest(newRequest.RequestId)
                        {
                            ProposedChanges = request.ProposedChanges,
                            ProposedPersonAzureUniqueId = request.ProposedPersonAzureUniqueId
                        });
                    }

                    newRequest = await DispatchAsync(new GetResourceAllocationRequestItem(newRequest.RequestId).ExpandAll());
                    requests.Add(newRequest!);
                }
                await transaction.CommitAsync();

                var getRequestUrl = Url.Action(nameof(GetResourceAllocationRequestsForProject), new { projectIdentifier = projectIdentifier.ProjectId });
                var createdAtUrl = getRequestUrl + $"?$filter=orgPositionId eq '{request.OrgPositionId}'";

                return Created(createdAtUrl, requests.Select(x => new ApiResourceAllocationRequest(x)).ToList());
            }
            catch (ValidationException ex)
            {
                return ApiErrors.InvalidOperation(ex);
            }
        }

        [HttpPost("/departments/{departmentPath}/resources/requests")]
        public async Task<ActionResult<ApiResourceAllocationRequest>> CreateResourceOwnerRequest(
            [FromRoute] string departmentPath, [FromBody] CreateResourceOwnerAllocationRequest request)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    or.BeResourceOwner(new DepartmentPath(departmentPath).Parent(), includeParents: false, includeDescendants: true);
                });
            });


            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            // Resolve position
            var position = await ResolvePositionAsync(request.OrgPositionId);
            var assignedPerson = position!.Instances
                .FirstOrDefault(i => i.Id == request.OrgPositionInstanceId)
                ?.AssignedPerson;

            if (assignedPerson is null)
                return ApiErrors.InvalidInput($"Cannot create change request for position instance without assigned person.");
            if (!assignedPerson.AzureUniqueId.HasValue)
                return ApiErrors.InvalidInput($"Cannot create change request for resource not in Active Directory.");

            var assignedPersonProfile = await profileResolver.ResolvePersonBasicProfileAsync(assignedPerson.AzureUniqueId!);
            if (!assignedPersonProfile?.FullDepartment?.Equals(departmentPath, StringComparison.OrdinalIgnoreCase) == true)
                return ApiErrors.InvalidInput($"The assigned resource does not belong to the department '{departmentPath}'");

            // Check if change requests are disabled.
            // This is mainly relevant when there is a mix of projects synced FROM pims and some TO pims. 
            // Change requests are only enabled on projects that have pims write sync enabled for now.
            var projectCheck = await IsChangeRequestsDisabledAsync(position.ProjectId);
            if (projectCheck.isDisabled)
            {
                return projectCheck.response;
            }

            var command = new CreateInternalRequest(InternalRequestOwner.ResourceOwner, request.ResolveType())
            {
                SubType = request.SubType,
                AdditionalNote = request.AdditionalNote,
                OrgPositionId = request.OrgPositionId,
                OrgProjectId = position!.ProjectId,
                OrgPositionInstanceId = request.OrgPositionInstanceId,
                AssignedDepartment = departmentPath
            };

            try
            {

                using var transaction = await BeginTransactionAsync();

                var newRequest = await DispatchAsync(command);

                if (request.ProposedChanges is not null || request.ProposedPersonAzureUniqueId is not null || request.ProposalParameters is not null)
                {
                    newRequest = await DispatchAsync(new UpdateInternalRequest(newRequest.RequestId)
                    {
                        ProposedChanges = request.ProposedChanges,
                        ProposedPersonAzureUniqueId = request.ProposedPersonAzureUniqueId,
                        ProposalChangeFrom = request.ProposalParameters?.ChangeDateFrom,
                        ProposalChangeTo = request.ProposalParameters?.ChangeDateTo,
                        ProposalScope = request.ProposalParameters?.ResolveScope() ?? ProposalChangeScope.Default,
                        ProposalChangeType = request.ProposalParameters?.Type
                    });
                }

                await transaction.CommitAsync();

                newRequest = await DispatchAsync(new GetResourceAllocationRequestItem(newRequest.RequestId).ExpandAll());
                return Created($"/departments/{departmentPath}/resources/requests/{newRequest!.RequestId}", new ApiResourceAllocationRequest(newRequest));
            }
            catch (ValidationException ex)
            {
                return ApiErrors.InvalidOperation(ex);
            }

        }

        [HttpPatch("/resources/requests/internal/{requestId}")]
        [HttpPatch("/projects/{projectIdentifier}/requests/{requestId}")]
        [HttpPatch("/projects/{projectIdentifier}/resources/requests/{requestId}")]
        [HttpPatch("/departments/{departmentString}/resources/requests/{requestId}")]
        public async Task<ActionResult<ApiResourceAllocationRequest>> PatchInternalRequest(
            [FromRoute] PathProjectIdentifier? projectIdentifier,
            string? departmentString,
            Guid requestId,
            [FromBody] PatchInternalRequestRequest request)
        {
            var item = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));

            if (item == null)
                return ApiErrors.NotFound("Could not locate request", $"{requestId}");
            if (item.IsCompleted)
                return ApiErrors.InvalidOperation("request-completed", "Cannot change a completed request.");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    if (item.OrgPositionId.HasValue)
                        or.OrgChartPositionWriteAccess(item.Project.OrgProjectId, item.OrgPositionId.Value);

                    if (!HasChanged(request.AdditionalNote, item.AdditionalNote))
                    {
                        if (item.AssignedDepartment is not null)
                        {
                            or.BeResourceOwner(
                                new DepartmentPath(item.AssignedDepartment).GoToLevel(2),
                                includeParents: false,
                                includeDescendants: true
                            );
                        }
                        else
                        {
                            or.BeResourceOwner();
                        }
                    }

                    or.BeRequestCreator(requestId);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();
            #endregion

            try
            {
                var updateCommand = new UpdateInternalRequest(requestId);

                if (request.AdditionalNote.HasValue) updateCommand.AdditionalNote = request.AdditionalNote.Value;
                if (request.AssignedDepartment.HasValue) updateCommand.AssignedDepartment = request.AssignedDepartment.Value;
                if (request.ProposedChanges.HasValue) updateCommand.ProposedChanges = request.ProposedChanges.Value;

                if (request.ProposedPersonAzureUniqueId.HasValue)
                {
                    if (!request.ProposedPersonAzureUniqueId.Value.HasValue && !CanUnsetProposedPerson(item))
                        return BadRequest("Cannot remove proposed person when request is not draft.");
                    updateCommand.ProposedPersonAzureUniqueId = request.ProposedPersonAzureUniqueId.Value;
                }
                if (request.ProposalParameters.HasValue)
                {
                    var @params = request.ProposalParameters.Value;

                    updateCommand.ProposalChangeFrom = @params.ChangeDateFrom;
                    updateCommand.ProposalChangeTo = @params.ChangeDateTo;
                    updateCommand.ProposalScope = @params.ResolveScope();
                    updateCommand.ProposalChangeType = @params.Type;
                }

                await using var scope = await BeginTransactionAsync();
                var updatedRequest = await DispatchAsync(updateCommand);
                await scope.CommitAsync();

                updatedRequest = await DispatchAsync(new GetResourceAllocationRequestItem(requestId).ExpandAll());
                return new ApiResourceAllocationRequest(updatedRequest!);
            }
            catch (ValidationException ve)
            {
                return ApiErrors.InvalidOperation(ve);
            }
        }

        private static bool CanUnsetProposedPerson(QueryResourceAllocationRequest item)
        {
            return item.IsDraft
                || item.State == AllocationNormalWorkflowV1.CREATED
                || item.State == AllocationNormalWorkflowV1.PROPOSAL;
        }

        [HttpGet("/resources/requests/internal")]
        public async Task<ActionResult<ApiCollection<ApiResourceAllocationRequest>>> GetAllRequests([FromQuery] ODataQueryParams query)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen()
                    .FullControl()
                    .FullControlInternal()
                    .BeTrustedApplication();

                r.AnyOf(or =>
                {
                    or.BeTrustedApplication();
                    or.BeResourceOwner();
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion


            var requestCommand = new GetResourceAllocationRequests(query).ForAll();
            var result = await DispatchAsync(requestCommand);


            var apiModel = result.Select(x => new ApiResourceAllocationRequest(x)).ToList();
            return new ApiCollection<ApiResourceAllocationRequest>(apiModel);
        }

        [HttpGet("/projects/{projectIdentifier}/requests")]
        [HttpGet("/projects/{projectIdentifier}/resources/requests")]
        public async Task<ActionResult<ApiCollection<ApiResourceAllocationRequest>>> GetResourceAllocationRequestsForProject(
            [FromRoute] PathProjectIdentifier projectIdentifier, [FromQuery] ODataQueryParams query)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen()
                    .FullControl()
                    .FullControlInternal()
                    .BeTrustedApplication();

                r.AnyOf(or =>
                {
                    // For now everyone with a position in the project can view requests
                    or.HaveOrgchartPosition(ProjectOrganisationIdentifier.FromOrgChartId(projectIdentifier.ProjectId));
                    or.OrgChartReadAccess(projectIdentifier.ProjectId);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var requestCommand = new GetResourceAllocationRequests(query)
                .ForTaskOwners()
                .WithProjectId(projectIdentifier.ProjectId);

            var result = await DispatchAsync(requestCommand);

            var apiModel = result.Select(x => new ApiResourceAllocationRequest(x)).ToList();

            // When querying by project, hide proposed values if type is allocation and state is in proposal.
            foreach (var request in apiModel.Where(x => x.ShouldHideProposalsForProject))
                request.HideProposals();

            return new ApiCollection<ApiResourceAllocationRequest>(apiModel);
        }

        [HttpGet("/resources/requests/internal/unassigned")]
        public async Task<ActionResult<ApiCollection<ApiResourceAllocationRequest>>> GetUnassignedRequests([FromQuery] ODataQueryParams query)
        {
            var countEnabled = Request.Query.ContainsKey("$count");

            var requestCommand = new GetResourceAllocationRequests(query)
                .ForResourceOwners()
                .WithUnassignedFilter(true)
                .WithExcludeCompleted(true)
                .WithOnlyCount(countEnabled);

            var result = await DispatchAsync(requestCommand);

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal().BeTrustedApplication();
                r.AnyOf(or =>
                {
                    or.BeResourceOwner();
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var apiModel = result.Select(x => new ApiResourceAllocationRequest(x)).ToList();

            var start = result.Skip;
            var end = start + result.Count;
            Request.Headers.Add("Accept-Ranges", "requests");
            Request.Headers.Add("Content-Range", $"requests {start}-{end}/{result.TotalCount}");

            return new ApiCollection<ApiResourceAllocationRequest>(apiModel)
            {
                TotalCount = countEnabled ? result.TotalCount : null
            };
        }

        [HttpGet("/resources/requests/internal/{requestId}")]
        [HttpGet("/projects/{projectIdentifier}/requests/{requestId}")]
        [HttpGet("/projects/{projectIdentifier}/resources/requests/{requestId}")]
        [HttpGet("/departments/{departmentString}/resources/requests/{requestId}")]
        public async Task<ActionResult<ApiResourceAllocationRequest>> GetResourceAllocationRequest(Guid requestId, PathProjectIdentifier? projectIdentifier, [FromQuery] ODataQueryParams query)
        {
            var result = await DispatchAsync(new GetResourceAllocationRequestItem(requestId).WithQuery(query));

            if (result == null)
                return ApiErrors.NotFound("Could not locate request", $"{requestId}");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal().BeTrustedApplication();
                r.AnyOf(or =>
                {
                    or.BeRequestCreator(requestId);
                    or.HaveOrgchartPosition(ProjectOrganisationIdentifier.FromOrgChartId(result.Project.OrgProjectId));
                    or.OrgChartReadAccess(result.Project.OrgProjectId);

                    if (result.OrgPositionId.HasValue)
                        or.OrgChartPositionReadAccess(result.Project.OrgProjectId, result.OrgPositionId.Value);

                    if (result.AssignedDepartment is not null)
                    {
                        or.BeResourceOwner(
                            new DepartmentPath(result.AssignedDepartment).GoToLevel(2),
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

            var apiModel = new ApiResourceAllocationRequest(result);

            if (projectIdentifier is null)
                return apiModel;

            return apiModel.ShouldHideProposalsForProject ? apiModel.HideProposals() : apiModel;
        }

        [HttpGet("/projects/{projectIdentifier}/positions/{positionId}/requests")]
        public async Task<ActionResult<List<ApiResourceAllocationRequest>>> GetRequestsForPosition(PathProjectIdentifier projectIdentifier, Guid positionId)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal().BeTrustedApplication();
                r.AnyOf(or =>
                {
                    or.HaveOrgchartPosition(ProjectOrganisationIdentifier.FromOrgChartId(projectIdentifier.ProjectId));
                    or.OrgChartReadAccess(projectIdentifier.ProjectId);
                    or.OrgChartPositionReadAccess(projectIdentifier.ProjectId, positionId);
                    or.BeResourceOwner();
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();
            #endregion

            var command = new GetResourceAllocationRequests()
                .WithProjectId(projectIdentifier.ProjectId)
                .WithPositionId(positionId)
                .ForAll();

            var result = await DispatchAsync(command);
            return Ok(result.Select(x => new ApiResourceAllocationRequest(x)));
        }

        [HttpPost("/projects/{projectIdentifier}/requests/{requestId}/start")]
        [HttpPost("/projects/{projectIdentifier}/resources/requests/{requestId}/start")]
        public async Task<ActionResult<ApiResourceAllocationRequest>> StartProjectRequestWorkflow([FromRoute] PathProjectIdentifier projectIdentifier, Guid requestId)
        {
            var result = await DispatchAsync(new GetResourceAllocationRequestItem(requestId).ExpandActions());

            if (result == null)
                return ApiErrors.NotFound("Could not locate request", $"{requestId}");

            if (result.Project.OrgProjectId != projectIdentifier.ProjectId)
                return ApiErrors.NotFound("Could not locate request in project", $"{requestId}");

            if (result.Actions?.Any(x => x.IsRequired && !x.IsResolved) == true)
                return ApiErrors.InvalidOperation("UnresolvedRequiredTask", "Cannot start the request when there are unresolved required tasks.");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    if (result.OrgPositionId.HasValue)
                        or.OrgChartPositionWriteAccess(result.Project.OrgProjectId, result.OrgPositionId.Value);
                    or.BeRequestCreator(requestId);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            try
            {
                await using var transaction = await BeginTransactionAsync();
                await DispatchAsync(new Logic.Commands.ResourceAllocationRequest.Initialize(requestId));
                await transaction.CommitAsync();
            }
            catch (InvalidWorkflowError ex)
            {
                return ApiErrors.InvalidOperation(ex);
            }

            result = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));
            return new ApiResourceAllocationRequest(result!);
        }

        [HttpPost("/departments/{departmentPath}/resources/requests/{requestId}/start")]
        public async Task<ActionResult<ApiResourceAllocationRequest>> StartResourceOwnerRequestWorkflow([FromRoute] string departmentPath, Guid requestId)
        {
            var result = await DispatchAsync(new GetResourceAllocationRequestItem(requestId).ExpandActions());

            if (result == null)
                return ApiErrors.NotFound("Could not locate request", $"{requestId}");

            if (result.Actions?.Any(x => x.IsRequired && !x.IsResolved) == true)
                return ApiErrors.InvalidOperation("UnresolvedRequiredTask", "Cannot start the request when there are unresolved required tasks.");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    or.BeResourceOwner(new DepartmentPath(departmentPath).Parent(), includeDescendants: true);
                    or.BeRequestCreator(requestId);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            try
            {
                await using var transaction = await BeginTransactionAsync();
                await DispatchAsync(new Logic.Commands.ResourceAllocationRequest.Initialize(requestId));
                await transaction.CommitAsync();
            }
            catch (InvalidWorkflowError ex)
            {
                return ApiErrors.InvalidOperation(ex);
            }

            result = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));
            return new ApiResourceAllocationRequest(result!);
        }

        [HttpDelete("/resources/requests/internal/{requestId}")]
        [HttpDelete("/projects/{projectIdentifier}/requests/{requestId}")]
        [HttpDelete("/projects/{projectIdentifier}/resources/requests/{requestId}")]
        [HttpDelete("/departments/{departmentString}/resources/requests/{requestId}")]
        public async Task<ActionResult> DeleteAllocationRequest(Guid requestId)
        {
            var result = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));

            if (result is null)
                return ApiErrors.NotFound("Could not locate request", $"{requestId}");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    if (result.Type == InternalRequestType.Allocation)
                    {
                        or.BeRequestCreator(requestId);

                        if (result.OrgPositionId.HasValue)
                            or.OrgChartPositionWriteAccess(result.Project.OrgProjectId, result.OrgPositionId.Value);
                    }
                });

            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion


            await using var transaction = await BeginTransactionAsync();
            await DispatchAsync(new DeleteInternalRequest(requestId));

            await transaction.CommitAsync();

            return NoContent();
        }

        [HttpPost("/resources/requests/internal/{requestId}/provision")]
        public async Task<ActionResult<ApiResourceAllocationRequest>> ProvisionProjectAllocationRequest(Guid requestId)
        {
            var result = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));

            if (result == null)
                return ApiErrors.NotFound("Could not locate request", $"{requestId}");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen()
                    .FullControl()
                    .FullControlInternal();

                r.AnyOf(or =>
                {
                    or.BeTrustedApplication();
                });

            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion


            try
            {
                await using var scope = await BeginTransactionAsync();

                await DispatchAsync(new Logic.Commands.ResourceAllocationRequest.Provision(requestId));

                await scope.CommitAsync();

                result = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));
                return new ApiResourceAllocationRequest(result!);
            }
            catch (ValidationException ex)
            {
                return ApiErrors.InvalidOperation(ex);
            }
        }


        [HttpPost("/projects/{projectIdentifier}/requests/{requestId}/approve")]
        [HttpPost("/projects/{projectIdentifier}/resources/requests/{requestId}/approve")]
        public async Task<ActionResult<ApiResourceAllocationRequest>> ApproveProjectAllocationRequest([FromRoute] PathProjectIdentifier projectIdentifier, Guid requestId)
        {
            var result = await DispatchAsync(new GetResourceAllocationRequestItem(requestId).ExpandActions());

            if (result == null)
                return ApiErrors.NotFound("Could not locate request", $"{requestId}");

            if (result.Actions?.Any(x => x.IsRequired && !x.IsResolved) == true)
                return ApiErrors.InvalidOperation("UnresolvedRequiredTask", "Cannot start the request when there are unresolved required tasks.");

            await using var scope = await BeginTransactionAsync();

            try
            {
                await DispatchAsync(new Logic.Commands.ResourceAllocationRequest.Approve(requestId));
                await scope.CommitAsync();
            }
            catch (UnauthorizedWorkflowException ex)
            {
                await scope.RollbackAsync();
                return new ObjectResult(ex.ToErrorObject()) { StatusCode = (int)HttpStatusCode.Forbidden };
            }

            result = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));
            
            return new ApiResourceAllocationRequest(result!);
        }

        [HttpPost("/departments/{departmentPath}/requests/{requestId}/approve")]
        [HttpPost("/departments/{departmentPath}/resources/requests/{requestId}/approve")]
        public async Task<ActionResult<ApiResourceAllocationRequest>> ApproveProjectAllocationRequest([FromRoute] string departmentPath, Guid requestId)
        {
            var result = await DispatchAsync(new GetResourceAllocationRequestItem(requestId).ExpandActions());

            if (result == null)
                return ApiErrors.NotFound("Could not locate request", $"{requestId}");
            //if (result.AssignedDepartment != departmentPath)
            //    return ApiErrors.InvalidInput($"The request with id '{requestId}' is not assigned to '{departmentPath}'");

            if (result.Actions?.Any(x => x.IsRequired && !x.IsResolved) == true)
                return ApiErrors.InvalidOperation("UnresolvedRequiredTask", "Cannot start the request when there are unresolved required tasks.");

            await using var scope = await BeginTransactionAsync();

            try
            {
                await DispatchAsync(new Logic.Commands.ResourceAllocationRequest.Approve(requestId));
                await scope.CommitAsync();
            }
            catch (UnauthorizedWorkflowException ex)
            {
                await scope.RollbackAsync();
                return new ObjectResult(ex.ToErrorObject()) { StatusCode = (int)HttpStatusCode.Forbidden };
            }

            result = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));
            return new ApiResourceAllocationRequest(result!);
        }

        [HttpDelete("/resources/requests/internal/{requestId}/workflow")]
        [HttpDelete("/departments/{departmentString}/resources/requests/{requestId}/workflow")]
        public async Task<ActionResult> ResetWorkflow(Guid requestId, string? departmentString)
        {
            var requestItem = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));

            if (requestItem == null)
                return ApiErrors.NotFound("Could not locate request", $"{requestId}");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    if (!string.IsNullOrEmpty(requestItem.AssignedDepartment))
                        or.BeResourceOwner(new DepartmentPath(requestItem.AssignedDepartment).Parent(), includeParents: false, includeDescendants: true);
                    else
                        or.BeResourceOwner();
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            await DispatchAsync(new ResetWorkflow(requestId));
            return NoContent();
        }


        #region Comments

        [HttpOptions("/resources/requests/internal/{requestId}/comments")]
        public async Task<ActionResult> GetCommentOptions(Guid requestId)
        {
            var request = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));

            if (request == null)
                return FusionApiError.NotFound(requestId, "Request not found");

            #region Authorization

            var requiredDepartment = request.AssignedDepartment ?? request.OrgPosition?.BasePosition?.Department;

            if (requiredDepartment is null)
                return FusionApiError.Forbidden("Cannot determine required department");

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    or.BeResourceOwner(new DepartmentPath(requiredDepartment).Parent(), includeParents: true, includeDescendants: true);
                });
            });
            #endregion

            var allowedMethods = new List<string> { "OPTIONS" };

            if (authResult.Success)
            {
                if (!request.IsCompleted) allowedMethods.Add("POST");
                allowedMethods.Add("GET");
            }

            Response.Headers["Allow"] = string.Join(',', allowedMethods);
            return NoContent();
        }

        [HttpOptions("/resources/requests/internal/{requestId}/comments/{commentId}")]
        public async Task<ActionResult> GetCommentOptions(Guid requestId, Guid commentId)
        {
            var request = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));
            var comment = await DispatchAsync(new GetRequestComment(commentId));

            if (request == null)
                return FusionApiError.NotFound(requestId, "Request not found");
            if (comment is null)
                return FusionApiError.NotFound(commentId, "Comment not found");

            #region Authorization
            var requiredDepartment = request.AssignedDepartment ?? request.OrgPosition?.BasePosition?.Department;

            if (requiredDepartment is null)
                return FusionApiError.Forbidden("Cannot determine required department");

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    or.BeResourceOwner(new DepartmentPath(requiredDepartment).Parent(), includeParents: true, includeDescendants: true);
                });
            });
            #endregion

            var allowedMethods = new List<string> { "OPTIONS" };

            if (!request.IsCompleted && authResult.Success)
            {
                allowedMethods.Add("GET", "PUT", "DELETE");
            }

            Response.Headers["Allow"] = string.Join(',', allowedMethods);
            return NoContent();
        }

        [HttpPost("/resources/requests/internal/{requestId}/comments")]
        public async Task<ActionResult<ApiRequestComment>> AddRequestComment(Guid requestId, [FromBody] RequestCommentRequest create)
        {
            var request = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));

            if (request == null)
                return FusionApiError.NotFound(requestId, "Request not found");
            if (request.IsCompleted)
                return FusionApiError.InvalidOperation("CommentsDisabled", "Cannot add comment on closed request");
            #region Authorization

            var requiredDepartment = request.AssignedDepartment ?? request.OrgPosition?.BasePosition?.Department;

            if (requiredDepartment is null)
                return Forbid("Cannot determine required department");

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    or.BeResourceOwner(new DepartmentPath(requiredDepartment).Parent(), includeParents: true, includeDescendants: true);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var comment = await DispatchAsync(new AddComment(User.GetRequestOrigin(), requestId, create.Content));

            return Created($"/resources/requests/internal/{requestId}/comments/{comment.Id}", new ApiRequestComment(comment));
        }

        [HttpGet("/resources/requests/internal/{requestId}/comments")]
        public async Task<ActionResult<IEnumerable<ApiRequestComment>>> GetRequestComment(Guid requestId)
        {
            var request = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));

            if (request == null)
                return FusionApiError.NotFound(requestId, "Request not found");

            if (request.IsCompleted)
                return FusionApiError.InvalidOperation("CommentsDisabled", "Comments are closed on completed requests.");

            #region Authorization
            var requiredDepartment = request.AssignedDepartment ?? request.OrgPosition?.BasePosition?.Department;

            if (requiredDepartment is null)
                return Forbid("Cannot determine required department");

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    or.BeResourceOwner(new DepartmentPath(requiredDepartment).Parent(), includeParents: true, includeDescendants: true);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var comments = await DispatchAsync(new GetRequestComments(requestId));
            return comments.Select(x => new ApiRequestComment(x)).ToList();
        }

        [HttpGet("/resources/requests/internal/{requestId}/comments/{commentId}")]
        public async Task<ActionResult<ApiRequestComment>> GetRequestComment(Guid requestId, Guid commentId)
        {
            var request = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));
            var comment = await DispatchAsync(new GetRequestComment(commentId));

            if (request == null)
                return FusionApiError.NotFound(requestId, "Request not found");

            if (comment is null)
                return FusionApiError.NotFound(commentId, "Comment not found");

            if (request.IsCompleted)
                return FusionApiError.InvalidOperation("CommentsDisabled", "Comments are closed on completed requests.");

            #region Authorization

            var requiredDepartment = request.AssignedDepartment ?? request.OrgPosition?.BasePosition?.Department;

            if (requiredDepartment is null)
                return Forbid("Cannot determine required department access");

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    or.BeResourceOwner(new DepartmentPath(requiredDepartment).Parent(), includeParents: true, includeDescendants: true);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            return new ApiRequestComment(comment);
        }

        [HttpPut("/resources/requests/internal/{requestId}/comments/{commentId}")]
        public async Task<ActionResult<ApiRequestComment>> UpdateRequestComment(Guid requestId, Guid commentId, [FromBody] RequestCommentRequest update)
        {
            var request = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));
            var comment = await DispatchAsync(new GetRequestComment(commentId));

            if (request == null)
                return FusionApiError.NotFound(requestId, "Request not found");

            if (comment is null)
                return FusionApiError.NotFound(commentId, "Comment not found");

            if (request.IsCompleted)
                return FusionApiError.InvalidOperation("CommentsDisabled", "Comments are closed on completed requests.");

            #region Authorization

            var requiredDepartment = request.AssignedDepartment ?? request.OrgPosition?.BasePosition?.Department;

            if (requiredDepartment is null)
                return Forbid("Cannot determine required department");

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    or.BeResourceOwner(new DepartmentPath(requiredDepartment).Parent(), includeParents: true, includeDescendants: true);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            await DispatchAsync(new UpdateComment(commentId, update.Content));

            comment = await DispatchAsync(new GetRequestComment(commentId));
            return new ApiRequestComment(comment!);
        }

        [HttpDelete("/resources/requests/internal/{requestId}/comments/{commentId}")]
        public async Task<ActionResult> DeleteRequestComment(Guid requestId, Guid commentId)
        {
            var request = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));
            var comment = await DispatchAsync(new GetRequestComment(commentId));

            if (request == null)
                return FusionApiError.NotFound(requestId, "Request not found");

            if (comment is null)
                return FusionApiError.NotFound(commentId, "Comment not found");

            #region Authorization

            var requiredDepartment = request.AssignedDepartment ?? request.OrgPosition?.BasePosition?.Department;

            if (requiredDepartment is null)
                return Forbid("Cannot determine required department");

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    or.BeResourceOwner(new DepartmentPath(requiredDepartment).Parent(), includeParents: true, includeDescendants: true);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            await DispatchAsync(new DeleteComment(commentId));

            return NoContent();
        }

        #endregion Comments

        [HttpOptions("/projects/{projectIdentifier}/requests/{requestId}/approve")]
        [HttpOptions("/projects/{projectIdentifier}/resources/requests/{requestId}/approve")]
        public async Task<ActionResult<ApiResourceAllocationRequest>> CheckApprovalAccess([FromRoute] PathProjectIdentifier projectIdentifier, Guid requestId)
        {
            var result = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));

            if (result == null)
                return ApiErrors.NotFound("Could not locate request", $"{requestId}");

            if (String.IsNullOrEmpty(result.State)) return NoContent();

            try
            {
                var canApprove = DispatchAsync(new Logic.Commands.ResourceAllocationRequest.CanApproveStep(requestId, result.Type.MapToDatabase(), result.State, null));
            }
            catch (UnauthorizedWorkflowException)
            {
                return NoContent();
            }

            Response.Headers["Allow"] = "POST";
            return NoContent();
        }

        [HttpOptions("/projects/{projectIdentifier}/requests/{requestId}")]
        [HttpOptions("/projects/{projectIdentifier}/resources/requests/{requestId}")]
        public async Task<ActionResult> CheckProjectAllocationRequestAccess([FromRoute] PathProjectIdentifier projectIdentifier, Guid requestId)
        {
            var allowedVerbs = new List<string>();
            var item = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));

            if (item == null)
                return ApiErrors.NotFound("Could not locate request", $"{requestId}");

            var patchResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    if (item.OrgPositionId.HasValue)
                        or.OrgChartPositionWriteAccess(item.Project.OrgProjectId, item.OrgPositionId.Value);

                    if (item.AssignedDepartment is not null)
                    {
                        or.BeResourceOwner(
                            new DepartmentPath(item.AssignedDepartment).GoToLevel(2),
                            includeParents: false,
                            includeDescendants: true
                        );
                    }
                    else
                    {
                        or.BeResourceOwner();
                    }

                    or.BeRequestCreator(requestId);
                });
            });
            if (patchResult.Success) allowedVerbs.Add("PATCH");

            var deleteResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    if (item.Type == InternalRequestType.Allocation)
                    {
                        or.BeRequestCreator(requestId);

                        if (item.OrgPositionId.HasValue)
                            or.OrgChartPositionWriteAccess(item.Project.OrgProjectId, item.OrgPositionId.Value);
                    }
                });
            });

            if (deleteResult.Success) allowedVerbs.Add("DELETE");

            var getResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal().BeTrustedApplication();
                r.AnyOf(or =>
                {
                    or.BeRequestCreator(requestId);
                    // For now everyone with a position in the project can view requests
                    or.HaveOrgchartPosition(ProjectOrganisationIdentifier.FromOrgChartId(item.Project.OrgProjectId));

                    if (item.OrgPositionId.HasValue)
                        or.OrgChartPositionReadAccess(item.Project.OrgProjectId, item.OrgPositionId.Value);


                    if (item.AssignedDepartment is not null)
                    {
                        or.BeResourceOwner(
                            new DepartmentPath(item.AssignedDepartment).GoToLevel(2),
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
            if (getResult.Success) allowedVerbs.Add("GET");

            Response.Headers["Allow"] = string.Join(',', allowedVerbs);
            return NoContent();
        }


        /// <summary>
        /// Check if request type is supported to create on the specific allocation.
        /// 
        /// The endpoint will return 'POST' in the 'Allow' header.
        /// </summary>
        /// <param name="projectIdentifier">Project the position exists on</param>
        /// <param name="positionId">Position id to create the request on</param>
        /// <param name="instanceId">Instance / allocation to target</param>
        /// <param name="requestType">The request type to create</param>
        /// <returns></returns>
        [HttpOptions("/projects/{projectIdentifier}/positions/{positionId}/instances/{instanceId}/resources/requests")]
        public async Task<ActionResult> CheckInstanceRequestTypeAsync([FromRoute] PathProjectIdentifier projectIdentifier, Guid positionId, Guid instanceId, [FromQuery] string? requestType)
        {

            switch (requestType?.ToLower())
            {
                case "resourceownerchange":
                    // Check if change requests are disabled.
                    // This is mainly relevant when there is a mix of projects synced FROM pims and some TO pims. 
                    // Change requests are only enabled on projects that have pims write sync enabled for now.
                    var projectCheck = await IsChangeRequestsDisabledAsync(projectIdentifier.ProjectId);
                    if (projectCheck.isDisabled)
                    {
                        // Creating custom response payload here, as bad request would be confusing with regards for unsupported request types. 
                        // Instead lets return ok response without any allow header, but with an error payload.
                        Response.Headers.Add("Allow", "");
                        return Ok(new { error = new { code = "ChangeRequestsDisabled", message = "The project does not currently support change requests from resource owners..." } });
                    }

                    break;

                default:
                    return ApiErrors.InvalidInput("Request type is not supported. Supported types are 'ResourceOwnerChange'");
            }

            Response.Headers.Add("Allow", "POST");
            return NoContent();
        }

        [HttpOptions("/departments/{departmentPath}/resources/requests/{requestId}")]
        public async Task<ActionResult> CheckDepartmentRequestAccess(string departmentPath, Guid requestId)
        {
            var allowedVerbs = new List<string>();
            var item = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));

            if (item is null) return NotFound();

            var getAuth = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal().BeTrustedApplication();
                r.AnyOf(or =>
                {
                    or.BeRequestCreator(requestId);
                    // For now everyone with a position in the project can view requests
                    or.HaveOrgchartPosition(ProjectOrganisationIdentifier.FromOrgChartId(item.Project.OrgProjectId));
                    or.OrgChartReadAccess(item.Project.OrgProjectId);

                    if (item.OrgPositionId.HasValue)
                        or.OrgChartPositionReadAccess(item.Project.OrgProjectId, item.OrgPositionId.Value);


                    if (item.AssignedDepartment is not null)
                    {
                        or.BeResourceOwner(
                            new DepartmentPath(item.AssignedDepartment).GoToLevel(2),
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
            if (getAuth.Success) allowedVerbs.Add("GET");

            var deleteAuth = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    if (item.Type == InternalRequestType.Allocation)
                    {
                        or.BeRequestCreator(requestId);

                        if (item.OrgPositionId.HasValue)
                            or.OrgChartPositionWriteAccess(item.Project.OrgProjectId, item.OrgPositionId.Value);
                    }
                });

            });
            if (deleteAuth.Success) allowedVerbs.Add("DELETE");

            var patchAuth = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    if (item.OrgPositionId.HasValue)
                        or.OrgChartPositionWriteAccess(item.Project.OrgProjectId, item.OrgPositionId.Value);

                    if (item.AssignedDepartment is not null)
                    {
                        or.BeResourceOwner(
                            new DepartmentPath(item.AssignedDepartment).GoToLevel(2),
                            includeParents: false,
                            includeDescendants: true
                        );
                    }
                    else
                    {
                        or.BeResourceOwner();
                    }
                    or.BeRequestCreator(requestId);
                });
            });
            if (patchAuth.Success) allowedVerbs.Add("PATCH");

            Response.Headers["Allow"] = String.Join(',', allowedVerbs);
            return NoContent();
        }

        [HttpOptions("/projects/{projectIdentifier}/requests")]
        [HttpOptions("/projects/{projectIdentifier}/resources/requests")]
        [HttpOptions("/departments/{departmentPath}/resources/requests")]
        public async Task<ActionResult<ApiCollection<ApiResourceAllocationRequest>>> GetResourceAllocationRequestsOptions(
            [FromRoute] PathProjectIdentifier projectIdentifier, [FromRoute] string? departmentPath)
        {
            var allowedVerbs = new List<string>();

            var postAuth = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    if (departmentPath is not null)
                        or.BeResourceOwner(departmentPath, includeParents: false, includeDescendants: true);
                });
            });
            if (postAuth.Success) allowedVerbs.Add("POST");


            var getAuth = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen()
                    .FullControl()
                    .FullControlInternal()
                    .BeTrustedApplication();

                r.AnyOf(or =>
                {
                    // For now everyone with a position in the project can view requests
                    or.HaveOrgchartPosition(ProjectOrganisationIdentifier.FromOrgChartId(projectIdentifier.ProjectId));
                    if (departmentPath is not null)
                        or.BeResourceOwner(departmentPath, includeParents: false, includeDescendants: true);
                });
            });

            if (getAuth.Success) allowedVerbs.Add("GET");

            Response.Headers["Allow"] = String.Join(',', allowedVerbs);

            return NoContent();
        }

        [HttpOptions("/departments/{departmentPath}/resources/requests/{requestId}/approve")]
        public async Task<ActionResult> GetWorkflowApprovalOptions(string departmentPath, Guid requestId)
        {
            var result = await DispatchAsync(new GetResourceAllocationRequestItem(requestId));

            if (result is null) return NotFound();
            if (String.IsNullOrEmpty(result.State)) return NoContent();

            try
            {
                var canApprove = DispatchAsync(new Logic.Commands.ResourceAllocationRequest.CanApproveStep(requestId, result.Type.MapToDatabase(), result.State, null));
            }
            catch (UnauthorizedWorkflowException)
            {
                return NoContent();
            }

            Response.Headers["Allow"] = "POST";
            return NoContent();
        }

        private bool HasChanged<T>(PatchProperty<T?> patchValue, T? originalValue)
           where T : IEquatable<T>
        {
            return patchValue.HasValue
                && !patchValue.Value!.Equals(originalValue);
        }
    }
}
