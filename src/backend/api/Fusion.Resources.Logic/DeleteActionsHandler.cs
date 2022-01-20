using Fusion.Resources.Database;
using Fusion.Resources.Logic.Events;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class DeleteActionsHandler : INotificationHandler<RequestProvisioned>
{
    private readonly ResourcesDbContext resourcesDb;

    public DeleteActionsHandler(ResourcesDbContext resourcesDb)
    {
        this.resourcesDb = resourcesDb;
    }
    public async Task Handle(RequestProvisioned notification, CancellationToken cancellationToken)
    {
        await DeleteActions(notification.RequestId, cancellationToken);
    }

    private Task DeleteActions(Guid requestId, CancellationToken cancellationToken)
    {
        resourcesDb.RequestActions.RemoveRange(
            resourcesDb.RequestActions.Where(c => c.RequestId == requestId)
        );
        return resourcesDb.SaveChangesAsync(cancellationToken);
    }
}