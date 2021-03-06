﻿using Fusion.Resources.Database;
using Fusion.Resources.Logic.Events;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Logic
{
    public class DeleteNotesHandler : INotificationHandler<RequestProvisioned>
    {
        private readonly ResourcesDbContext resourcesDb;

        public DeleteNotesHandler(ResourcesDbContext resourcesDb)
        {
            this.resourcesDb = resourcesDb;
        }
        public async Task Handle(RequestProvisioned notification, CancellationToken cancellationToken)
        {
            await DeleteNotes(notification.RequestId, cancellationToken);
        }

        private Task DeleteNotes(Guid requestId, CancellationToken cancellationToken)
        {
            resourcesDb.RequestComments.RemoveRange(
                resourcesDb.RequestComments.Where(c => c.RequestId == requestId)
            );
            return resourcesDb.SaveChangesAsync(cancellationToken);
        }
    }
}
