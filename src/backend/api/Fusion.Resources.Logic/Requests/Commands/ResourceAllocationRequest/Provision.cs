using Fusion.ApiClients.Org;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
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
                     .Include(p => p.Project)
                     .FirstOrDefaultAsync(r => r.Id == request.RequestId);

                    if (dbRequest is null)
                        throw new RequestNotFoundError(request.RequestId);

                    switch (dbRequest.State)
                    {
                        case DbResourceAllocationRequestState.Created:
                            await CreatePositionAsync(dbRequest);
                            break;

                        default:
                            await UpdatePositionAsync(dbRequest);
                            break;
                    }

                    await resourcesDb.SaveChangesAsync();
                }

                private async Task CreatePositionAsync(DbResourceAllocationRequest dbRequest)
                {
                    /*var createPositionCommand = new CreatePosition(dbRequest.Project.OrgProjectId, Guid.Empty)
                    {
                        AppliesFrom = dbRequest.OrgPositionInstance.AppliesFrom,
                        AppliesTo = dbRequest.OrgPositionInstance.AppliesTo,
                        //PositionName = dbRequest.OrgPositionInstance.Name,
                        Workload = dbRequest.OrgPositionInstance.Workload.GetValueOrDefault(0),
                        Obs = dbRequest.OrgPositionInstance.Obs,
                        //BasePositionId = dbRequest.OrgPositionInstance.BasePositionId,
                        AssignedPerson = dbRequest.ProposedPerson.Mail
                        //ParentPositionId = dbRequest.OrgPositionInstance.TaskOwner.PositionId
                    };*/

                    dbRequest.ProvisioningStatus.Provisioned = DateTime.UtcNow;
                    dbRequest.LastActivity = DateTime.UtcNow;

                    try
                    {
                     //   var position = await mediator.Send(createPositionCommand);
                        dbRequest.ProvisioningStatus.State = DbResourceAllocationRequest.DbProvisionState.Provisioned;
                        //dbRequest.ProvisioningStatus.PositionId = position.Id;

                        dbRequest.ProvisioningStatus.ErrorMessage = null;
                        dbRequest.ProvisioningStatus.ErrorPayload = null;
                    }
                    catch (OrgApiError apiError)
                    {
                        dbRequest.ProvisioningStatus.ErrorMessage = $"Received error from Org service when trying to create the position: '{apiError.Error?.Message}'";
                        dbRequest.ProvisioningStatus.ErrorPayload = apiError.ResponseText;
                        dbRequest.ProvisioningStatus.State = DbResourceAllocationRequest.DbProvisionState.Error;
                    }
                }

                private async Task UpdatePositionAsync(DbResourceAllocationRequest dbRequest)
                {
                    if (dbRequest.OriginalPositionId == null)
                        throw new InvalidOperationException("Cannot provision change request when original position id is empty.");

                    /*var updatePositionCommand = new UpdatePosition(dbRequest.Project.OrgProjectId, Guid.Empty, dbRequest.OriginalPositionId.Value)
                    {
                        AppliesFrom = dbRequest.OrgPositionInstance.AppliesFrom,
                        AppliesTo = dbRequest.OrgPositionInstance.AppliesTo,
                        //PositionName = dbRequest.OrgPositionInstance.Name,
                        Workload = dbRequest.OrgPositionInstance.Workload.GetValueOrDefault(0),
                        Obs = dbRequest.OrgPositionInstance.Obs,
                        //BasePositionId = dbRequest.OrgPositionInstance.BasePositionId,
                        AssignedPerson = dbRequest.ProposedPerson.Mail,
                        //ParentPositionId = dbRequest.Position.TaskOwner.PositionId
                    };*/

                    dbRequest.ProvisioningStatus.Provisioned = DateTime.UtcNow;
                    dbRequest.LastActivity = DateTime.UtcNow;

                    try
                    {
                        //var position = await mediator.Send(updatePositionCommand);
                        dbRequest.ProvisioningStatus.State = DbResourceAllocationRequest.DbProvisionState.Provisioned;
                        //dbRequest.ProvisioningStatus.PositionId = position.Id;

                        dbRequest.ProvisioningStatus.ErrorMessage = null;
                        dbRequest.ProvisioningStatus.ErrorPayload = null;
                    }
                    catch (OrgApiError apiError)
                    {
                        dbRequest.ProvisioningStatus.ErrorMessage = $"Received error from Org service when trying to create the position: '{apiError.Error?.Message}'";
                        dbRequest.ProvisioningStatus.ErrorPayload = apiError.ResponseText;
                        dbRequest.ProvisioningStatus.State = DbResourceAllocationRequest.DbProvisionState.Error;
                    }
                }
            }
        }

    }


}
