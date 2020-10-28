using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain.Commands;
using Fusion.Resources.Domain.Notifications.Request;
using Fusion.Resources.Logic.Workflows;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ContractorPersonnelRequest
    {

        public class SetState : TrackableRequest
        {
            public SetState(Guid requestId, DbRequestState state)
            {
                RequestId = requestId;
                State = state;
            }


            public Guid RequestId { get; set; }
            public DbRequestState State { get; set; }
            public string? Reason { get; set; }

            public SetState WithReason(string reason)
            {
                Reason = reason;
                return this;
            }

            public class Handler : AsyncRequestHandler<SetState>
            {
                private readonly ResourcesDbContext resourcesDb;
                private readonly IMediator mediator;
                private INotification? notifyOnSave = null;

                public Handler(ResourcesDbContext resourcesDb, IMediator mediator)
                {
                    this.resourcesDb = resourcesDb;
                    this.mediator = mediator;
                }

                private DbContractorRequest dbItem = null!;
                private ContractorPersonnelWorkflowV1 workflow = null!;

                protected override async Task Handle(SetState request, CancellationToken cancellationToken)
                {
                    dbItem = await resourcesDb.ContractorRequests
                        .Include(r => r.Project)
                        .Include(r => r.Contract)
                        .FirstOrDefaultAsync(r => r.Id == request.RequestId);

                    if (dbItem == null)
                        throw new RequestNotFoundError(request.RequestId);



                    var dbWorkflow = await mediator.GetRequestWorkflowAsync(dbItem.Id);
                    workflow = new ContractorPersonnelWorkflowV1(dbWorkflow);


                    switch (dbItem.State)
                    {
                        case DbRequestState.Created:
                            await HandleWhenCreatedAsync(request);
                            break;

                        case DbRequestState.SubmittedToCompany:
                            await HandleWhenSubmittedToCompanyAsync(request);
                            break;

                        case DbRequestState.RejectedByCompany:
                        case DbRequestState.RejectedByContractor:
                        case DbRequestState.ApprovedByCompany:
                        default:
                            throw new IllegalStateChangeError(dbItem.State, request.State);
                    }

                    dbItem.State = request.State;
                    dbItem.LastActivity = DateTime.UtcNow;

                    // Update the encapsulated dbentity with the new workflow state.
                    workflow.SaveChanges();

                    await resourcesDb.SaveChangesAsync();

                    if (notifyOnSave != null)
                        await mediator.Publish(notifyOnSave);
                }

                private async ValueTask HandleWhenSubmittedToCompanyAsync(SetState request)
                {
                    switch (request.State)
                    {
                        case DbRequestState.ApprovedByCompany:
                            workflow.CompanyApproved(request.Editor.Person);
                            await mediator.Send(QueueRequestProvisioning.ContractorPersonnelRequest(request.RequestId, dbItem.Project.OrgProjectId, dbItem.Contract.OrgContractId));
                            notifyOnSave = new RequestApprovedByCompany(request.RequestId, request.Editor.Person);
                            break;

                        case DbRequestState.RejectedByCompany:
                            if (request.Reason is null)
                                throw new ArgumentException("Reason", "Reason must be specified when rejecting request");

                            workflow.CompanyRejected(request.Editor.Person, request.Reason);
                            notifyOnSave = new RequestDeclinedByCompany(request.RequestId, request.Reason, request.Editor.Person);
                            break;

                        default:
                            throw new IllegalStateChangeError(dbItem.State, request.State, DbRequestState.ApprovedByCompany, DbRequestState.RejectedByCompany);
                    }

                }
                private ValueTask HandleWhenCreatedAsync(SetState request)
                {
                    switch (request.State)
                    {
                        case DbRequestState.SubmittedToCompany:
                            workflow.ContractorApproved(request.Editor.Person);
                            notifyOnSave = new RequestApprovedByContractor(request.RequestId, request.Editor.Person);
                            break;

                        case DbRequestState.RejectedByContractor:
                            if (request.Reason is null)
                                throw new ArgumentException("Reason", "Reason must be specified when rejecting request");

                            workflow.ContractorRejected(request.Editor.Person, request.Reason);
                            notifyOnSave = new RequestDeclinedByContractor(request.RequestId, request.Reason, request.Editor.Person);
                            break;

                        default:
                            throw new IllegalStateChangeError(dbItem.State, request.State, DbRequestState.SubmittedToCompany, DbRequestState.RejectedByContractor);
                    }

                    return new ValueTask();
                }
            }
        }
    }


}
