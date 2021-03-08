using FluentValidation;
using Fusion.ApiClients.Org;
using Fusion.Integration.Org;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
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

            public class Validator : AbstractValidator<Provision>
            {
                public Validator(ResourcesDbContext db, IProjectOrgResolver projectOrgResolver)
                {
                    //RuleFor(x => x.RequestId).MustAsync(async (id, cancel) =>
                    //{
                    //    return await db.ResourceAllocationRequests.AnyAsync(y =>
                    //        y.Id == id && y.Type == DbResourceAllocationRequest.DbAllocationRequestType.Direct);
                    //}).WithMessage($"Request must exist.");

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


                    switch (dbRequest.Type)
                    {
                        case DbInternalRequestType.Direct:
                        case DbInternalRequestType.JointVenture:
                        case DbInternalRequestType.Normal:
                            await ProvisionAllocationRequestAsync(dbRequest);
                            break;

                        default:
                            throw new NotSupportedException($"Provisioning for request of type {dbRequest.Type} is not supported");
                    }

                    await resourcesDb.SaveChangesAsync();
                }

                private async Task ProvisionAllocationRequestAsync(DbResourceAllocationRequest dbRequest)
                {
                    if (dbRequest.OrgPositionId == null)
                        throw new InvalidOperationException("Cannot provision change request when original position id is empty.");

                    if (dbRequest.ProposedChanges != null || dbRequest.ProposedPerson != null)
                    {
                        var patchDoc = CreatePatchPositionInstanceV2(dbRequest.ProposedChanges, dbRequest.ProposedPerson?.AzureUniqueId);
                        var updatePositionCommand = new UpdatePositionInstance(dbRequest.Project.OrgProjectId, dbRequest.OrgPositionId.Value, dbRequest.OrgPositionInstance.Id, patchDoc);

                        dbRequest.ProvisioningStatus.Provisioned = DateTime.UtcNow;
                        dbRequest.LastActivity = DateTime.UtcNow;
                        try
                        {
                            var position = await mediator.Send(updatePositionCommand);
                            dbRequest.ProvisioningStatus.State = DbResourceAllocationRequest.DbProvisionState.Provisioned;
                            dbRequest.ProvisioningStatus.OrgPositionId = position.Id;
                            dbRequest.ProvisioningStatus.OrgProjectId = dbRequest.Project.OrgProjectId;
                            dbRequest.ProvisioningStatus.OrgInstanceId = dbRequest.OrgPositionInstance.Id;

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
                        dbRequest.ProvisioningStatus.ErrorMessage = $"Request payload of proposed changes and proposed person was null or empty. Unable to provision";
                        dbRequest.ProvisioningStatus.State = DbResourceAllocationRequest.DbProvisionState.Error;
                    }
                }

                /// <summary>
                /// Based upon ApiPositionInstanceV2. Consider re-mapping if updating API version.
                /// </summary>
                /// <param name="changes">Proposed changes json</param>
                /// <param name="proposedPerson">Proposed person</param>
                /// <returns></returns>
                private static PatchPositionInstanceV2 CreatePatchPositionInstanceV2(string? changes, Guid? personAzureUniqueId)
                {
                    var proposedChanges = new JObject();

                    if (!string.IsNullOrEmpty(changes))
                        proposedChanges = JObject.Parse(changes);

                    var patchDoc = new PatchPositionInstanceV2();
                    if (personAzureUniqueId != null)
                        patchDoc.AssignedPerson = new ApiPersonV2 { AzureUniqueId = personAzureUniqueId };

                    if (proposedChanges.TryGetValue("obs", StringComparison.InvariantCultureIgnoreCase, out var obs))
                        patchDoc.Obs = obs.ToObject<string?>();

                    if (proposedChanges.TryGetValue("workload", StringComparison.InvariantCultureIgnoreCase, out var workload))
                        patchDoc.Workload = workload.ToObject<double?>();

                    if (proposedChanges.TryGetValue("appliesFrom", StringComparison.InvariantCultureIgnoreCase, out var appliesFrom))
                        patchDoc.AppliesFrom = appliesFrom.ToObject<DateTime?>();

                    if (proposedChanges.TryGetValue("appliesTo", StringComparison.InvariantCultureIgnoreCase, out var appliesTo))
                        patchDoc.AppliesTo = appliesTo.ToObject<DateTime?>();

                    if (proposedChanges.TryGetValue("location", StringComparison.InvariantCultureIgnoreCase, out var location))
                        patchDoc.Location = location.ToObject<ApiPositionLocationV2?>()!;

                    return patchDoc;
                }
            }
        }

    }
}