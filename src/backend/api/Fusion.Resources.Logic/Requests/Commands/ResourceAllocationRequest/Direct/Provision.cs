using Fusion.ApiClients.Org;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
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
                    public Validator(ResourcesDbContext db)
                    {
                        RuleFor(x => x.RequestId).MustAsync(async (id, cancel) =>
                        {
                            return await db.ResourceAllocationRequests.AnyAsync(y =>
                                y.Id == id && y.Type == DbResourceAllocationRequest.DbAllocationRequestType.Direct);
                        }).WithMessage($"Request must exist.");

                        // Valider om instansen eksisterer
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
                            .FirstOrDefaultAsync(r => r.Id == request.RequestId);


                        if (dbRequest is null)
                            throw new RequestNotFoundError(request.RequestId);


                        await UpdatePositionAsync(dbRequest);

                        await resourcesDb.SaveChangesAsync();
                    }

                    private async Task UpdatePositionAsync(DbResourceAllocationRequest dbRequest)
                    {
                        if (dbRequest.OrgPositionId == null)
                            throw new InvalidOperationException(
                                "Cannot provision change request when original position id is empty.");

                        ///Kommando for å oppdatere en versjon av posisjons-instansen
                        /// I stedenfor UpdatePosition => Assign TBN Position Instance
                        ///
                        /// Bruk versjon 2 av Org - sørg for å mappe proposedchanges dict.
                        /// Beskrive i kommandoen at det er versjon-2 som brukes
                        ///
                        /// 
                        /// 
                        ///dbRequest.ProposedChanges
                        var updatePositionCommand = new UpdatePosition(dbRequest.Project.OrgProjectId, dbRequest.OrgPositionId.Value)
                        {
                            AppliesFrom = dbRequest.OrgPositionInstance.AppliesFrom,
                            AppliesTo = dbRequest.OrgPositionInstance.AppliesTo,
                            Workload = dbRequest.OrgPositionInstance.Workload.GetValueOrDefault(),
                            Obs = dbRequest.OrgPositionInstance.Obs,
                            AssignedPerson = dbRequest.ProposedPerson!.AzureUniqueId
                        };

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
                }
            }
        }
    }
}
