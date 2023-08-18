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
                    if (notification.Workflow is not AllocationDirectWorkflowV1 workflow)
                        return;

                    var request = await dbContext.ResourceAllocationRequests
                        .FirstAsync(r => r.Id == notification.RequestId, cancellationToken);
                    
                    await ValidateWorkflowAsync(request);


                    // Check for auto approval
                    if (request.ProposedPerson.AzureUniqueId.HasValue)
                    {
                        var autoApprovalEnabledForResource = await mediator.Send(new Domain.Queries.GetPersonAutoApprovalStatus(request.ProposedPerson.AzureUniqueId!.Value));
                        if (autoApprovalEnabledForResource == true)
                        {
                            workflow.AutoComplete();
                            workflow.SaveChanges();

                            // No more steps to do
                            await mediator.Send(new QueueProvisioning(request.Id));
                        }

                        // Save changes for workflow definition.
                        await dbContext.SaveChangesAsync();
                    }


                }

                private async Task ValidateWorkflowAsync(DbResourceAllocationRequest request)
                {
                    if (!request.ProposedPerson.HasBeenProposed)
                        throw InvalidWorkflowError.ValidationError<AllocationDirectWorkflowV1>("Cannot start direct request without a person proposed", s =>
                            s.AddFailure("proposedPerson", "Must provide a person to be assigned the position"));

                    // Need to resolve the person which has been proposed, as that determine if department must be assigned.
                    var profile = await mediator.Send(new Domain.Queries.GetPersonProfile(PersonId.Create(request.ProposedPerson.AzureUniqueId, request.ProposedPerson.Mail)));

                    if (profile is null)
                        throw InvalidWorkflowError.ValidationError<AllocationDirectWorkflowV1>("Cannot start direct request, the proposed person does not exist", s =>
                            s.AddFailure("proposedPerson", "Person must exist"));

                    if (profile.IsExpired == true)
                        throw InvalidWorkflowError.ValidationError<AllocationDirectWorkflowV1>("Cannot start direct request, the proposed person account has expired", s =>
                            s.AddFailure("proposedPerson", "Proposed person account must be valid."));

                    // Guest accounts do not have a manger or a department, so the request does not need to be assigned anywhere.
                    if (profile.AccountType != Fusion.Integration.Profile.FusionAccountType.External && request.AssignedDepartment is null)
                        throw InvalidWorkflowError.ValidationError<AllocationDirectWorkflowV1>("Cannot start direct request without assigned department", s =>
                            s.AddFailure("assignedDepartment", "Must provide assigned department to the request"));


                }
            }
        }
    }
}
