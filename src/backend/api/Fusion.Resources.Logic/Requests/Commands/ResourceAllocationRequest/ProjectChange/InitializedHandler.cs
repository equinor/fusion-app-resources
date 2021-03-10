using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain;
using Fusion.Resources.Logic.Workflows;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {

        public partial class ProjectChange
        {
            public class InitializedHandler : INotificationHandler<RequestInitialized>
            {
                private readonly ResourcesDbContext dbContext;

                public InitializedHandler(ResourcesDbContext dbContext)
                {
                    this.dbContext = dbContext;
                }

                public async Task Handle(RequestInitialized notification, CancellationToken cancellationToken)
                {
                    if (notification.Type != DbInternalRequestType.TaskOwnerChange)
                        return;

                    var initiatedBy = await dbContext.Persons.FirstAsync(p => p.Id == notification.InitiatedByDbPersonId);
                    var request = await dbContext.ResourceAllocationRequests.FirstAsync(r => r.Id == notification.RequestId);

                    ValidateWorkflow(request);

                    StartWorkflow(request, initiatedBy);

                    await dbContext.SaveChangesAsync();
                }

                private void ValidateWorkflow(DbResourceAllocationRequest request)
                {
                    var hasChanges = !string.IsNullOrEmpty(request.ProposedChanges);
                    var hasPersonChange = request.ProposedPerson.HasBeenProposed;

                    // Check that the workflow can be started. This requires that a person is proposed.
                    if (!hasChanges && !hasPersonChange)
                        throw InvalidWorkflowError.ValidationError<InternalRequestTaskOwnerChangeWorkflowV1>("Cannot start change request without any proposed changes");
                }

                private void StartWorkflow(DbResourceAllocationRequest request, DbPerson initiatedBy)
                {
                    var workflow = new InternalRequestTaskOwnerChangeWorkflowV1(initiatedBy);
                    dbContext.Workflows.Add(workflow.CreateDatabaseEntity(request.Id, DbRequestType.InternalRequest));

                    request.LastActivity = DateTime.UtcNow;
                    request.State.State = InternalRequestTaskOwnerChangeWorkflowV1.CREATED;
                }
            }

        }
    }
}