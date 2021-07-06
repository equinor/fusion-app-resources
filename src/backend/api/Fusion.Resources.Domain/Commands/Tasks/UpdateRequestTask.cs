using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text.Json;
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
        public MonitorableProperty<string> Type { get; set; } = new();
        public MonitorableProperty<string?> SubType { get; set; } = new();
        public MonitorableProperty<bool> IsResolved { get; set; } = new();
        public MonitorableProperty<Dictionary<string, object>?> Properties { get; set; } = new();

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

                if (task is null) throw new TaskNotFoundError(request.requestId, request.taskId);

                request.Title.IfSet(title => task.Title = title);
                request.Body.IfSet(body => task.Body = body);

                request.Type.IfSet(type => task.Type = type);
                request.SubType.IfSet(subType => task.SubType = subType);

                request.Properties.IfSet(props => UpdateCustomProperties(task, props));

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

                await db.SaveChangesAsync(cancellationToken);

                return new QueryRequestTask(task);
            }

            private static void UpdateCustomProperties(Database.Entities.DbRequestTask task, Dictionary<string, object>? props)
            {
                if (props is null) return;

                var existingProps = new QueryRequestTask(task).Properties;
                foreach(var prop in props)
                {
                    if (existingProps.ContainsKey(prop.Key))
                    {
                        existingProps[prop.Key] = prop.Value;
                    }
                    else
                    {
                        existingProps.Add(prop.Key, prop.Value);
                    }
                }
                task.PropertiesJson = JsonSerializer.Serialize(existingProps);
            }
        }
    }
}
