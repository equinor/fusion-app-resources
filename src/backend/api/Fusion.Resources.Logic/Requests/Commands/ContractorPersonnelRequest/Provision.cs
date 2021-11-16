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
    public partial class ContractorPersonnelRequest
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
                    var dbRequest = await resourcesDb.ContractorRequests
                     .Include(p => p.Person).ThenInclude(p => p.Person)
                     .Include(p => p.Project)
                     .Include(p => p.Contract)
                     .FirstOrDefaultAsync(r => r.Id == request.RequestId);

                    if (dbRequest is null)
                        throw new RequestNotFoundError(request.RequestId);

                    // Set headers for all api calls made to the org api from the point on. This will flag the change to come from resources.
                    using var orgChangeSourceMarker = new RequestHeadersScope()
                        .WithChangeSource("Resources External", $"{dbRequest.Id}");

                    switch (dbRequest.Category)
                    {
                        case DbRequestCategory.NewRequest:
                            await CreatePositionAsync(dbRequest);
                            break;

                        case DbRequestCategory.ChangeRequest:
                            await UpdatePositionAsync(dbRequest);
                            break;
                    }

                    await resourcesDb.SaveChangesAsync();
                }

                private async Task CreatePositionAsync(DbContractorRequest dbRequest)
                {
                    var createPositionCommand = new CreateContractPosition(dbRequest.Project.OrgProjectId, dbRequest.Contract.OrgContractId)
                    {
                        AppliesFrom = dbRequest.Position.AppliesFrom,
                        AppliesTo = dbRequest.Position.AppliesTo,
                        PositionName = dbRequest.Position.Name,
                        Workload = dbRequest.Position.Workload,
                        Obs = dbRequest.Position.Obs,
                        BasePositionId = dbRequest.Position.BasePositionId,
                        AssignedPerson =  dbRequest.Person.Person,
                        ParentPositionId = dbRequest.Position.TaskOwner.PositionId
                    };

                    dbRequest.ProvisioningStatus.Provisioned = DateTime.UtcNow;
                    dbRequest.LastActivity = DateTime.UtcNow;

                    try
                    {
                        var position = await mediator.Send(createPositionCommand);
                        dbRequest.ProvisioningStatus.State = DbContractorRequest.DbProvisionState.Provisioned;
                        dbRequest.ProvisioningStatus.PositionId = position.Id;

                        dbRequest.ProvisioningStatus.ErrorMessage = null;
                        dbRequest.ProvisioningStatus.ErrorPayload = null;
                    }
                    catch (OrgApiError apiError)
                    {
                        dbRequest.ProvisioningStatus.ErrorMessage = $"Received error from Org service when trying to create the position: '{apiError.Error?.Message}'";
                        dbRequest.ProvisioningStatus.ErrorPayload = apiError.ResponseText;
                        dbRequest.ProvisioningStatus.State = DbContractorRequest.DbProvisionState.Error;
                    }
                }

                private async Task UpdatePositionAsync(DbContractorRequest dbRequest)
                {
                    if (dbRequest.OriginalPositionId == null)
                        throw new InvalidOperationException("Cannot provision change request when original position id is empty.");

                    var updatePositionCommand = new UpdateContractPosition(dbRequest.Project.OrgProjectId, dbRequest.Contract.OrgContractId, dbRequest.OriginalPositionId.Value)
                    {
                        AppliesFrom = dbRequest.Position.AppliesFrom,
                        AppliesTo = dbRequest.Position.AppliesTo,
                        PositionName = dbRequest.Position.Name,
                        Workload = dbRequest.Position.Workload,
                        Obs = dbRequest.Position.Obs,
                        BasePositionId = dbRequest.Position.BasePositionId,
                        AssignedPerson = dbRequest.Person.Person,
                        ParentPositionId = dbRequest.Position.TaskOwner.PositionId
                    };

                    dbRequest.ProvisioningStatus.Provisioned = DateTime.UtcNow;
                    dbRequest.LastActivity = DateTime.UtcNow;

                    try
                    {
                        var position = await mediator.Send(updatePositionCommand);
                        dbRequest.ProvisioningStatus.State = DbContractorRequest.DbProvisionState.Provisioned;
                        dbRequest.ProvisioningStatus.PositionId = position.Id;

                        dbRequest.ProvisioningStatus.ErrorMessage = null;
                        dbRequest.ProvisioningStatus.ErrorPayload = null;
                    }
                    catch (OrgApiError apiError)
                    {
                        dbRequest.ProvisioningStatus.ErrorMessage = $"Received error from Org service when trying to create the position: '{apiError.Error?.Message}'";
                        dbRequest.ProvisioningStatus.ErrorPayload = apiError.ResponseText;
                        dbRequest.ProvisioningStatus.State = DbContractorRequest.DbProvisionState.Error;
                    }
                }
            }
        }

    }


}
