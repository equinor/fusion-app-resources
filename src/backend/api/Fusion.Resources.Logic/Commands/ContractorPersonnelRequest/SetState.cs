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

                public Handler(ResourcesDbContext resourcesDb)
                {
                    this.resourcesDb = resourcesDb;
                }

                protected override async Task Handle(SetState request, CancellationToken cancellationToken)
                {
                    var dbItem = await resourcesDb.ContractorRequests.FirstOrDefaultAsync(r => r.Id == request.RequestId);

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
                                    // Schedule provisioning
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
