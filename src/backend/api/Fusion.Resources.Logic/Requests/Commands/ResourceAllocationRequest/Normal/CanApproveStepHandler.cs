using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {

        public partial class Normal
        {
            public class CanApproveStepHandler : INotificationHandler<CanApproveStep>
            {
                private readonly ResourcesDbContext dbContext;

                public CanApproveStepHandler(ResourcesDbContext dbContext)
                {
                    this.dbContext = dbContext;
                }

                public async Task Handle(CanApproveStep notification, CancellationToken cancellationToken)
                {
                    if (notification.Type != DbInternalRequestType.Normal)
                        return;

                    //var initiatedBy = await dbContext.Persons.FirstAsync(p => p.Id == notification.InitiatedByDbPersonId);
                    var request = await dbContext.ResourceAllocationRequests.FirstAsync(r => r.Id == notification.RequestId);

                    switch (notification.CurrentStepId)
                    {
                        case "proposed":

                            break;
                    }                    
                }
            }
        }
    }
}