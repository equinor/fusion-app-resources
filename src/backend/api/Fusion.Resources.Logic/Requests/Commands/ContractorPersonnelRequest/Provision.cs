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

                    var createPositionCommand = new CreateContractPosition(dbRequest.Project.OrgProjectId, dbRequest.Contract.OrgContractId)
                    {
                        AppliesFrom = dbRequest.Position.AppliesFrom,
                        AppliesTo = dbRequest.Position.AppliesTo,
                        PositionName = dbRequest.Position.Name,
                        Workload = dbRequest.Position.Workload,
                        Obs = dbRequest.Position.Obs,
                        BasePositionId = dbRequest.Position.BasePositionId,
                        AssignedPerson = dbRequest.Person.Person.Mail,
                        ParentPositionId = dbRequest.Position.TaskOwner.PositionId
                    };

                    dbRequest.ProvisioningStatus.Provisioned = DateTime.UtcNow;

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

                    await resourcesDb.SaveChangesAsync();
                }
            }
        }

    }


}
