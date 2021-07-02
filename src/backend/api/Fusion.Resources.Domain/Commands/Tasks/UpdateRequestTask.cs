using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands.Tasks
{
    public class UpdateRequestTask : TrackableRequest<QueryRequestTask>
    {
        private Guid requestId;
        private Guid taskId;

        public UpdateRequestTask(Guid requestId, Guid taskId)
        {
            this.requestId = requestId;
            this.taskId = taskId;
        }

        public MonitorableProperty<string> Title { get; set; } = new();
        public MonitorableProperty<string> Body { get; set; } = new();
        public MonitorableProperty<string> Category { get; set; } = new();
        public MonitorableProperty<string> Type { get; set; } = new();
        public MonitorableProperty<string?> SubType { get; set; } = new();
        public MonitorableProperty<bool> IsResolved { get; set; } = new();

        public class Handler : IRequestHandler<UpdateRequestTask, QueryRequestTask>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            public async Task<QueryRequestTask> Handle(UpdateRequestTask request, CancellationToken cancellationToken)
            {
                var task = await db.RequestTasks
                    .Include(t => t.ResolvedBy)
                    .SingleOrDefaultAsync(t => t.Id == request.taskId && t.RequestId == request.requestId, cancellationToken);

                if (task is null) throw new Exception();

                request.Title.IfSet(title => task.Title = title);
                request.Body.IfSet(body => task.Body = body);

                request.Category.IfSet(category => task.Category = category);
                request.Type.IfSet(type => task.Type = type);
                request.SubType.IfSet(subType => task.SubType = subType);

                request.IsResolved.IfSet(isResolved =>
                {
                    if (isResolved)
                    {
                        task.IsResolved = isResolved;
                        task.ResolvedAt = DateTimeOffset.Now;
                        task.ResolvedBy = request.Editor.Person;
                        task.ResolvedById = request.Editor.Person.Id;
                    }
                    else
                    {
                        task.IsResolved = isResolved;
                        task.ResolvedAt = null;
                        task.ResolvedBy = null;
                        task.ResolvedById = null;
                    }
                });

                return new QueryRequestTask(task);
            }
        }
    }
}
