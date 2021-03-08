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

        public partial class Normal
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
                    if (notification.Type != InternalRequestType.Normal)
                        return;

                    var initiatedBy = await dbContext.Persons.FirstAsync(p => p.Id == notification.InitiatedByDbPersonId);
                    var request = await dbContext.ResourceAllocationRequests.FirstAsync(r => r.Id == notification.RequestId);

                   
                    var workflow = new InternalRequestNormalWorkflowV1(initiatedBy);
                    dbContext.Workflows.Add(workflow.CreateDatabaseEntity(notification.RequestId, DbRequestType.InternalRequest));

                    request.LastActivity = DateTime.UtcNow;
                    request.State.State = InternalRequestNormalWorkflowV1.CREATED;

                    await dbContext.SaveChangesAsync();
                }
            }

        }
    }
}