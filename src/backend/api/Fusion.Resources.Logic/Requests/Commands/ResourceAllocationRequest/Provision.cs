using FluentValidation;
using Fusion.ApiClients.Org;
using Fusion.Integration.Org;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain.Commands;
using Fusion.Resources.Logic.Events;
using Fusion.Resources.Logic.Workflows;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {
        public class Provision : TrackableRequest
        {
            public Provision(Guid requestId)
            {
                RequestId = requestId;
            }

            public Guid RequestId { get; }

            /// <summary>
            /// Force provisioning the request. This will skip validation and just execute the provision step.
            /// </summary>
            public bool ForceProvision { get; set; } = false;

            public class Validator : AbstractValidator<Provision>
            {
                public Validator(ResourcesDbContext db, IProjectOrgResolver projectOrgResolver, IMediator mediator)
                {
                    // Based on request, does the org position instance exist ?
                    RuleFor(x => x.RequestId).MustAsync(async (id, cancel) =>
                    {
                        var request = await db.ResourceAllocationRequests.FirstOrDefaultAsync(y => y.Id == id);

                        if (request.OrgPositionId == null)
                            return false;

                        var position = await projectOrgResolver.ResolvePositionAsync(request.OrgPositionId.Value);
                        var instance = position?.Instances.FirstOrDefault(x => x.Id == request.OrgPositionInstance.Id);
                        return instance != null;

                    }).WithMessage($"Org position instance must exist.");

                    RuleFor(x => x).MustAsync(async (field, req, ctx, cancel) =>
                    {
                        // Check that the next step is actually provision
                        var dbWorkflow = await mediator.GetRequestWorkflowAsync(req.RequestId);
                        var workflow = WorkflowDefinition.ResolveWorkflow(dbWorkflow);

                        if (!req.ForceProvision && workflow.GetCurrent().Id != WorkflowDefinition.PROVISIONING)
                        {
                            return false;
                        }

                        return true;

                    }).WithMessage("Current workflow step is not provisioning. Use force flag to override validation.");
                }
            }

            public class Handler : AsyncRequestHandler<Provision>
            {
                private readonly ILogger<Handler> logger;
                private readonly ResourcesDbContext resourcesDb;
                private readonly IMediator mediator;

                public Handler(ILogger<Handler> logger, ResourcesDbContext resourcesDb, IMediator mediator)
                {
                    this.logger = logger;
                    this.resourcesDb = resourcesDb;
                    this.mediator = mediator;
                }

                protected override async Task Handle(Provision request, CancellationToken cancellationToken)
                {
                    var dbRequest = await resourcesDb.ResourceAllocationRequests
                        .Include(r => r.Project)
                        .FirstOrDefaultAsync(r => r.Id == request.RequestId);


                    if (dbRequest is null)
                        throw new RequestNotFoundError(request.RequestId);


                    try
                    {
                        await ExecuteProvisioningAsync(dbRequest);
                        await UpdateWorkflowStatusAsync(request, dbRequest);
                    }
                    catch (ProvisioningError pEx)
                    {
                        logger.LogCritical(pEx, "Could not provision request: {0}", pEx.Message);
                    }

                    await resourcesDb.SaveChangesAsync();

                    if (dbRequest.ProvisioningStatus.State == DbResourceAllocationRequest.DbProvisionState.Provisioned)
                    {
                        await mediator.Publish(new RequestProvisioned(request.RequestId), cancellationToken);
                    }
                    else
                    {
                        await mediator.Publish(new RequestProvisioningFailed(request.RequestId), cancellationToken);
                    }
                }


                private async Task ExecuteProvisioningAsync(DbResourceAllocationRequest dbRequest)
                {
                    dbRequest.ProvisioningStatus.Provisioned = DateTime.UtcNow;

                    try
                    {
                        using var changeSourceHeaders = SetOrgApiChangeSource(dbRequest);

                        switch (dbRequest.Type)
                        {
                            case DbInternalRequestType.Allocation:
                                await mediator.Send(new Allocation.ProvisionAllocationRequest(dbRequest.Id));
                                break;

                            case DbInternalRequestType.ResourceOwnerChange:
                                await mediator.Send(new ResourceOwner.ProvisionResourceOwnerRequest(dbRequest.Id));
                                break;

                            default:
                                throw new NotSupportedException($"Provisioning for request of type {dbRequest.Type} is not supported");
                        }

                        dbRequest.ProvisioningStatus.State = DbResourceAllocationRequest.DbProvisionState.Provisioned;
                        dbRequest.ProvisioningStatus.OrgPositionId = dbRequest.OrgPositionId;
                        dbRequest.ProvisioningStatus.OrgProjectId = dbRequest.Project.OrgProjectId;
                        dbRequest.ProvisioningStatus.OrgInstanceId = dbRequest.OrgPositionInstance.Id;

                        dbRequest.ProvisioningStatus.ErrorMessage = null;
                        dbRequest.ProvisioningStatus.ErrorPayload = null;
                    }
                    catch (OrgApiError apiError)
                    {
                        dbRequest.ProvisioningStatus.ErrorMessage = $"Received error from Org service when trying to update the position: '{apiError.Error?.Message}'";
                        dbRequest.ProvisioningStatus.ErrorPayload = apiError.ResponseText;
                        dbRequest.ProvisioningStatus.State = DbResourceAllocationRequest.DbProvisionState.Error;

                        throw new ProvisioningError($"Error communicating with org chart: {apiError.Message}", apiError);
                    }
                }


                private async Task UpdateWorkflowStatusAsync(Provision request, DbResourceAllocationRequest dbRequest)
                {
                    var dbWorkflow = await mediator.GetRequestWorkflowAsync(dbRequest.Id);
                    var workflow = WorkflowDefinition.ResolveWorkflow(dbWorkflow);

                    // Assumes the next step is provisioning.
                    workflow
                        .Step(WorkflowDefinition.PROVISIONING)
                        .SetName("Provisioned")
                        .SetDescription($"Changes has been published to the org chart.")
                        .Complete(request.Editor.Person, true)
                        .CompleteWorkflow();
                    workflow.SaveChanges();
                    await resourcesDb.SaveChangesAsync();

                    dbRequest.State.IsCompleted = true;
                    dbRequest.State.State = "completed";
                }

                private static IDisposable SetOrgApiChangeSource(DbResourceAllocationRequest request)
                {
                    switch (request.Type)
                    {
                        case DbInternalRequestType.Allocation:
                            return new RequestHeadersScope().WithChangeSource("Resources Allocation", $"{request.RequestNumber}");
                        case DbInternalRequestType.ResourceOwnerChange:
                            return new RequestHeadersScope().WithChangeSource("Resources Change", $"{request.RequestNumber}");
                        default:
                            return new RequestHeadersScope().WithChangeSource($"Resources {request.Type}", $"{request.RequestNumber}");
                            
                    }
                }
            }
        }

        public class ProvisioningError : Exception
        {
            public ProvisioningError(string message, Exception? inner = null) : base(message, inner) { }
        }
    }
}