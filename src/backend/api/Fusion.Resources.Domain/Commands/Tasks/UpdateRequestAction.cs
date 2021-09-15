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
        public MonitorableProperty<string?> Body { get; set; } = new();
        public MonitorableProperty<string> Type { get; set; } = new();
        public MonitorableProperty<string?> SubType { get; set; } = new();
        public MonitorableProperty<bool> IsResolved { get; set; } = new();
        public MonitorableProperty<bool> IsRequired { get; set; } = new();

        public MonitorableProperty<DateTime?> DueDate { get; set; } = new();
        public MonitorableProperty<Guid?> AssignedToId { get; set; } = new();

        public MonitorableProperty<Dictionary<string, object>?> Properties { get; set; } = new();

        public class Handler : IRequestHandler<UpdateRequestAction, QueryRequestAction>
        {
            private readonly ResourcesDbContext db;
            private readonly IProfileService profileService;

            public Handler(ResourcesDbContext db, IProfileService profileService)
            {
                this.db = db;
                this.profileService = profileService;
            }

            public async Task<QueryRequestAction> Handle(UpdateRequestAction request, CancellationToken cancellationToken)
            {
                var action = await db.RequestActions
                    .Include(t => t.ResolvedBy)
                    .Include(t => t.SentBy)
                    .SingleOrDefaultAsync(t => t.Id == request.taskId && t.RequestId == request.requestId, cancellationToken);

                if (action is null) throw new ActionNotFoundError(request.requestId, request.taskId);

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

                await request.AssignedToId.IfSetAsync(async x => {
                    if (!x.HasValue) return;

                    var assignedTo = await profileService.EnsurePersonAsync(x.Value);
                    action.AssignedToId = assignedTo!.Id;
                });
                request.DueDate.IfSet(x => action.DueDate = x);

                await db.SaveChangesAsync(cancellationToken);

                return new QueryRequestAction(action);
            }

            private static void UpdateCustomProperties(Database.Entities.DbRequestAction action, Dictionary<string, object>? props)
            {
                if (props is null) return;

                var existingProps = new QueryRequestAction(action).Properties;
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
                action.PropertiesJson = JsonSerializer.Serialize(existingProps);
            }
        }
    }
}
