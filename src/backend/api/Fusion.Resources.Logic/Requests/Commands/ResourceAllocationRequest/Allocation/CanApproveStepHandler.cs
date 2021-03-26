using Fusion.Resources.Database.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Database;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {
        public class CanApproveStepHandler : INotificationHandler<CanApproveStep>
        {
            private readonly ResourcesDbContext dbContext;

            public CanApproveStepHandler(ResourcesDbContext dbContext)
            {
                this.dbContext = dbContext;
            }

            public Task Handle(CanApproveStep notification, CancellationToken cancellationToken)
            {
                if (notification.Type != DbInternalRequestType.Allocation)
                    return Task.CompletedTask;

                // Should be implemented when we need to authorize / validate. 
                // But at the moment these rules are not set, so would be back and forth to implement


                //var initiatedBy = await dbContext.Persons.FirstAsync(p => p.Id == notification.InitiatedByDbPersonId);
                //var request = await dbContext.ResourceAllocationRequests.FirstAsync(r => r.Id == notification.RequestId);

                switch (notification.CurrentStepId)
                {
                    case "proposed":

                        break;
                }

                return Task.CompletedTask;
            }
        }
    }
}
