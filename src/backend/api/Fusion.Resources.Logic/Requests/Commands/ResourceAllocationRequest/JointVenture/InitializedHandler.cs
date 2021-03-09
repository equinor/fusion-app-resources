using Fusion.Resources.Database.Entities;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Database;
using Fusion.Resources.Domain;
using Microsoft.EntityFrameworkCore;
using Fusion.Resources.Logic.Workflows;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {
        public partial class JointVenture
        {
            public class InitializedHandler : INotificationHandler<RequestInitialized>
            {
                private readonly ResourcesDbContext dbContext;
                private readonly IMediator mediator;

                public InitializedHandler(ResourcesDbContext dbContext, IMediator mediator)
                {
                    this.dbContext = dbContext;
                    this.mediator = mediator;
                }

                public async Task Handle(RequestInitialized notification, CancellationToken cancellationToken)
                {
                    if (notification.Type != InternalRequestType.JointVenture)
                        return;

                    var initiatedBy = await dbContext.Persons.FirstAsync(p => p.Id == notification.InitiatedByDbPersonId);
                    var request = await dbContext.ResourceAllocationRequests.FirstAsync(r => r.Id == notification.RequestId);


                    ValidateWorkflow(request);

                    var workflow = InitiateWorkflow(request, initiatedBy);

                    var wasAssigned = await AssignRequestToResourceOwnerAsync();

                    if (!wasAssigned)
                    {
                        workflow.SkipApproval();

                        // No more steps to do
                        await mediator.Send(new QueueProvisioning(request.Id));
                    }

                    await dbContext.SaveChangesAsync();
                }


                private void ValidateWorkflow(DbResourceAllocationRequest request)
                {
                    // Check that the workflow can be started. This requires that a person is proposed.
                    if (!request.ProposedPerson.HasBeenProposed)
                        throw InvalidWorkflowError.ValidationError<InternalRequestJointVentureWorkflowV1>("Cannot start joint venture request without a person proposed", s =>
                            s.AddFailure("proposedPerson", "Must provide a person to be assigned the position"));
                }

                private InternalRequestJointVentureWorkflowV1 InitiateWorkflow(DbResourceAllocationRequest request, DbPerson initiatedBy)
                {
                    var workflow = new InternalRequestJointVentureWorkflowV1(initiatedBy);
                    dbContext.Workflows.Add(workflow.CreateDatabaseEntity(request.Id, DbRequestType.InternalRequest));

                    request.LastActivity = DateTime.UtcNow;
                    request.State.State = InternalRequestJointVentureWorkflowV1.CREATED;

                    return workflow;
                }
            
                /// <summary>
                /// Assign the request to a resource owner if we can locate one. 
                /// </summary>
                /// <returns>true if an owner could be located</returns>
                private ValueTask<bool> AssignRequestToResourceOwnerAsync()
                {

                    return new ValueTask<bool>(false);
                }
            }
        }
    }
}
