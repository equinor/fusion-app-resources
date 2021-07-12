using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Logic.Workflows;
using Microsoft.EntityFrameworkCore;
using Fusion.Resources.Domain;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {
        public partial class Allocation
        {
            public class DirectAllocationRequestStarted : INotificationHandler<AllocationRequestStarted>
            {
                private readonly ResourcesDbContext dbContext;
                private readonly IMediator mediator;
                private readonly IRequestRouter router;

                public DirectAllocationRequestStarted(ResourcesDbContext dbContext, IMediator mediator, IRequestRouter router)
                {
                    this.dbContext = dbContext;
                    this.mediator = mediator;
                    this.router = router;
                }

                public async Task Handle(AllocationRequestStarted notification, CancellationToken cancellationToken)
                {
                    if (notification.Workflow is not AllocationDirectWorkflowV1)
                        return;

                    var request = await dbContext.ResourceAllocationRequests
                        .FirstAsync(r => r.Id == notification.RequestId, cancellationToken);

                    if(string.IsNullOrEmpty(request.AssignedDepartment))
                    {
                        request.AssignedDepartment = await router.RouteAsync(request, cancellationToken);
                    }
                    
                    ValidateWorkflow(request);
                }

                private static void ValidateWorkflow(DbResourceAllocationRequest request)
                {
                    if (request.AssignedDepartment is null)
                        throw InvalidWorkflowError.ValidationError<AllocationJointVentureWorkflowV1>("Cannot start direct request without assigned department", s =>
                            s.AddFailure("assignedDepartment", "Must provide assigned department to the request"));

                    if (!request.ProposedPerson.HasBeenProposed)
                        throw InvalidWorkflowError.ValidationError<AllocationJointVentureWorkflowV1>("Cannot start direct request without a person proposed", s =>
                            s.AddFailure("proposedPerson", "Must provide a person to be assigned the position"));
                }
            }
        }
    }
}
