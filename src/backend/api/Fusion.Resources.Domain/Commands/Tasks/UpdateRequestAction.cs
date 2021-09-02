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
    public class UpdateRequestAction : TrackableRequest<QueryRequestAction>
    {
        private Guid requestId;
        private Guid taskId;

        public UpdateRequestAction(Guid requestId, Guid taskId)
        {
            this.requestId = requestId;
            this.taskId = taskId;
        }

        public MonitorableProperty<string> Title { get; set; } = new();
        public MonitorableProperty<string> Body { get; set; } = new();
        public MonitorableProperty<string> Type { get; set; } = new();
        public MonitorableProperty<string?> SubType { get; set; } = new();
        public MonitorableProperty<bool> IsResolved { get; set; } = new();
        public MonitorableProperty<bool> IsRequired { get; set; } = new();
        public MonitorableProperty<Dictionary<string, object>?> Properties { get; set; } = new();

        public class Handler : IRequestHandler<UpdateRequestAction, QueryRequestAction>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            public async Task<QueryRequestAction> Handle(UpdateRequestAction request, CancellationToken cancellationToken)
            {
                var action = await db.RequestActions
                    .Include(t => t.ResolvedBy)
                    .Include(t => t.SentBy)
                    .SingleOrDefaultAsync(t => t.Id == request.taskId && t.RequestId == request.requestId, cancellationToken);

                if (action is null) throw new TaskNotFoundError(request.requestId, request.taskId);

                request.Title.IfSet(title => action.Title = title);
                request.Body.IfSet(body => action.Body = body);

                request.Type.IfSet(type => action.Type = type);
                request.SubType.IfSet(subType => action.SubType = subType);

                request.Properties.IfSet(props => UpdateCustomProperties(action, props));
                
                request.IsRequired.IfSet(isRequired => action.IsRequired = isRequired);
                request.IsResolved.IfSet(isResolved =>
                {
                    if (isResolved)
                    {
                        action.IsResolved = isResolved;
                        action.ResolvedAt = DateTimeOffset.Now;
                        action.ResolvedBy = request.Editor.Person;
                        action.ResolvedById = request.Editor.Person.Id;
                    }
                    else
                    {
                        action.IsResolved = isResolved;
                        action.ResolvedAt = null;
                        action.ResolvedBy = null;
                        action.ResolvedById = null;
                    }
                });

                await db.SaveChangesAsync(cancellationToken);

                return new QueryRequestAction(action);
            }

            private static void UpdateCustomProperties(Database.Entities.DbRequestAction task, Dictionary<string, object>? props)
            {
                if (props is null) return;

                var existingProps = new QueryRequestAction(task).Properties;
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
