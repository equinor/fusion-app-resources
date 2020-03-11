using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ContractorPersonnelRequest
    {
        public class SetState : IRequest
        {
            public SetState(Guid requestId, DbRequestState state)
            {
                RequestId = requestId;
                State = state;
            }


            public Guid RequestId { get; set; }
            public DbRequestState State { get; set; }


            public class Handler : AsyncRequestHandler<SetState>
            {
                private readonly ResourcesDbContext resourcesDb;
                private readonly IMediator mediator;

                public Handler(ResourcesDbContext resourcesDb, IMediator mediator)
                {
                    this.resourcesDb = resourcesDb;
                    this.mediator = mediator;
                }

                protected override async Task Handle(SetState request, CancellationToken cancellationToken)
                {
                    var dbItem = await resourcesDb.ContractorRequests
                        .Include(r => r.Project)
                        .Include(r => r.Contract)
                        .FirstOrDefaultAsync(r => r.Id == request.RequestId);

                    if (dbItem == null)
                        throw new RequestNotFoundError(request.RequestId);

                    switch (dbItem.State)
                    {
                        case DbRequestState.Created:

                            switch (request.State)
                            {
                                case DbRequestState.SubmittedToCompany:
                                    break;
                                case DbRequestState.RejectedByContractor:
                                    break;

                                default:
                                    throw new IllegalStateChangeError(dbItem.State, request.State, DbRequestState.SubmittedToCompany, DbRequestState.RejectedByContractor);
                            }

                            break;

                        case DbRequestState.SubmittedToCompany:

                            switch (request.State)
                            {
                                case DbRequestState.ApprovedByCompany:
                                    // Send notifications
                                    await mediator.Send(QueueRequestProvisioning.ContractorPersonnelRequest(request.RequestId, dbItem.Project.OrgProjectId, dbItem.Contract.OrgContractId));
                                     
                                    break;
                                case DbRequestState.RejectedByCompany:
                                    // Send notifications
                                    break;

                                default:
                                    throw new IllegalStateChangeError(dbItem.State, request.State, DbRequestState.ApprovedByCompany, DbRequestState.RejectedByCompany);
                            }

                            break;

                        case DbRequestState.RejectedByCompany:
                        case DbRequestState.RejectedByContractor:
                        case DbRequestState.ApprovedByCompany:
                        default:
                            throw new IllegalStateChangeError(dbItem.State, request.State);
                    }
                    
                    dbItem.State = request.State;

                    await resourcesDb.SaveChangesAsync();
                }
            }
        }
    }

    
}
