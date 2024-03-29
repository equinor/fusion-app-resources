﻿using Fusion.Resources.Database;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands.Tasks
{
    public class DeleteRequestAction : IRequest<bool>
    {
        private Guid requestId;
        private Guid taskId;

        public DeleteRequestAction(Guid requestId, Guid taskId)
        {
            this.requestId = requestId;
            this.taskId = taskId;
        }

        public class Handler : IRequestHandler<DeleteRequestAction, bool>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            public async Task<bool> Handle(DeleteRequestAction request, CancellationToken cancellationToken)
            {
                var query = db.RequestActions
                    .Where(t => t.Id == request.taskId && t.RequestId == request.requestId);
                db.RemoveRange(query);

                return await db.SaveChangesAsync(cancellationToken) >= 1;
            }
        }
    }
}
