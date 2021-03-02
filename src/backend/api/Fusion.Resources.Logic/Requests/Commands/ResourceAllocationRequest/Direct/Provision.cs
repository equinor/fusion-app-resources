using Fusion.ApiClients.Org;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Fusion.Integration.Org;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {
        public partial class Direct
        {
            public class Provision : TrackableRequest
            {
                public Provision(Guid requestId)
                {
                    RequestId = requestId;
                }

                public Guid RequestId { get; }

                public class Validator : AbstractValidator<Provision>
                {
                    public Validator(ResourcesDbContext db, IProjectOrgResolver projectOrgResolver)
                    {
                        RuleFor(x => x.RequestId).MustAsync(async (id, cancel) =>
                        {
                            return await db.ResourceAllocationRequests.AnyAsync(y =>
                                y.Id == id && y.Type == DbResourceAllocationRequest.DbAllocationRequestType.Direct);
                        }).WithMessage($"Request must exist.");

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
                    }
                }

                public class Handler : AsyncRequestHandler<Provision>
                {
                    private readonly ResourcesDbContext resourcesDb;
                    private readonly IMediator mediator;

                    public Handler(ResourcesDbContext resourcesDb, IMediator mediator)
                    {
                        this.resourcesDb = resourcesDb;
                        this.mediator = mediator;
                    }

                    protected override async Task Handle(Provision request, CancellationToken cancellationToken)
                    {
                        var dbRequest = await resourcesDb.ResourceAllocationRequests
                            .Include(r => r.Project)
                            .Include(r => r.ProposedPerson)
                            .FirstOrDefaultAsync(r => r.Id == request.RequestId);


                        if (dbRequest is null)
                            throw new RequestNotFoundError(request.RequestId);


                        await UpdatePositionAsync(dbRequest);

                        await resourcesDb.SaveChangesAsync();
                    }

                    private async Task UpdatePositionAsync(DbResourceAllocationRequest dbRequest)
                    {
                        if (dbRequest.OrgPositionId == null)
                            throw new InvalidOperationException("Cannot provision change request when original position id is empty.");

                        if (dbRequest.ProposedChanges != null)
                        {
                            var patchDoc = CreatePatchPositionInstanceV2(dbRequest);
                            var updatePositionCommand = new UpdatePositionInstance(dbRequest.Project.OrgProjectId, dbRequest.OrgPositionId.Value, dbRequest.OrgPositionInstance.Id, patchDoc);

                            dbRequest.ProvisioningStatus.Provisioned = DateTime.UtcNow;
                            dbRequest.LastActivity = DateTime.UtcNow;
                            try
                            {
                                var position = await mediator.Send(updatePositionCommand);
                                dbRequest.ProvisioningStatus.State = DbResourceAllocationRequest.DbProvisionState.Provisioned;
                                dbRequest.ProvisioningStatus.PositionId = position.Id;

                                dbRequest.ProvisioningStatus.ErrorMessage = null;
                                dbRequest.ProvisioningStatus.ErrorPayload = null;
                            }
                            catch (OrgApiError apiError)
                            {
                                dbRequest.ProvisioningStatus.ErrorMessage =
                                    $"Received error from Org service when trying to create the position: '{apiError.Error?.Message}'";
                                dbRequest.ProvisioningStatus.ErrorPayload = apiError.ResponseText;
                                dbRequest.ProvisioningStatus.State = DbResourceAllocationRequest.DbProvisionState.Error;
                            }
                        }
                        else
                        {
                            dbRequest.ProvisioningStatus.ErrorMessage = $"Request payload of proposed changes was null or empty. Unable to provision";
                            dbRequest.ProvisioningStatus.State = DbResourceAllocationRequest.DbProvisionState.Error;
                        }
                    }

                    /// <summary>
                    /// Based upon ApiPositionInstanceV2. Consider re-mapping if updating API version.
                    /// </summary>
                    /// <param name="dbRequest"></param>
                    /// <returns></returns>
                    private static PatchPositionInstanceV2 CreatePatchPositionInstanceV2(DbResourceAllocationRequest dbRequest)
                    {
                        var patchDoc = new PatchPositionInstanceV2();

                        var changedProps = JsonSerializerExtensions.DeserializeAnonymousType(dbRequest.ProposedChanges!,
                                new
                                {
                                    Obs = (string?)null,
                                    Workload = (double?)null,
                                    AppliesFrom = (DateTime?)null,
                                    AppliesTo = (DateTime?)null,
                                    Location = new
                                    {
                                        id = (Guid?)null
                                    }
                                },
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (changedProps != null)
                        {
                            patchDoc.Obs = changedProps.Obs;
                            patchDoc.Workload = changedProps.Workload;
                            patchDoc.AppliesFrom = changedProps.AppliesFrom;
                            patchDoc.AppliesTo = changedProps.AppliesTo;
                            if (changedProps.Location?.id != null)
                                patchDoc.Location = new ApiPositionLocationV2 { Id = changedProps.Location.id.Value };
                        }

                        if (dbRequest.ProposedPerson != null)
                        {
                            patchDoc.AssignedPerson = new ApiPersonV2 { AzureUniqueId = dbRequest.ProposedPerson.AzureUniqueId };
                        }


                        return patchDoc;
                    }
                }
            }
        }
    }
}
