using Fusion.Resources.Database.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Database;
using Fusion.Resources.Logic.Workflows;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {
        public partial class Allocation
        {
            public class JointVentureAllocationRequestStarted : INotificationHandler<AllocationRequestStarted>
            {
                private readonly ResourcesDbContext dbContext;
                private readonly IMediator mediator;

                public JointVentureAllocationRequestStarted(ResourcesDbContext dbContext, IMediator mediator)
                {
                    this.dbContext = dbContext;
                    this.mediator = mediator;
                }

                public async Task Handle(AllocationRequestStarted notification, CancellationToken cancellationToken)
                {
                    if (notification.Workflow is not AllocationJointVentureWorkflowV1 workflow)
                        return;

                    var request = await dbContext.ResourceAllocationRequests.FirstAsync(r => r.Id == notification.RequestId);


                    ValidateWorkflow(request);


                    var wasAssigned = await AssignRequestToResourceOwnerAsync();

                    if (!wasAssigned)
                    {
                        workflow.SkipApproval();
                        workflow.SaveChanges();

                        // No more steps to do
                        await mediator.Send(new QueueProvisioning(request.Id));
                    }

                    await dbContext.SaveChangesAsync();
                }

                private void ValidateWorkflow(DbResourceAllocationRequest request)
                {
                    // Check that the workflow can be started. This requires that a person is proposed.
                    if (!request.ProposedPerson.HasBeenProposed)
                        throw InvalidWorkflowError.ValidationError<AllocationJointVentureWorkflowV1>("Cannot start joint venture request without a person proposed", s =>
                            s.AddFailure("proposedPerson", "Must provide a person to be assigned the position"));
                }

                private ValueTask<bool> AssignRequestToResourceOwnerAsync()
                {

                    return new ValueTask<bool>(false);
                }
            }
        }
    }
}
