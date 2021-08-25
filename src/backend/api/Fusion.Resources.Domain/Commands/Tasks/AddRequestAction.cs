using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    public class AddRequestAction : IRequest<QueryRequestAction>
    {
        public AddRequestAction(Guid requestId, string title, string body, string type)
        {
            RequestId = requestId;
            Title = title;
            Body = body;
            Type = type;
        }

        public Guid RequestId { get; }
        public string Title { get; }
        public string Body { get; set; }
        public string Type { get; set; }
        public string? SubType { get; set; }
        public QueryTaskSource Source { get; set; }
        public QueryTaskResponsible Responsible { get; set; }
        public Dictionary<string, object>? Properties { get; set; }

        public class Handler : IRequestHandler<AddRequestAction, QueryRequestAction>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            public async Task<QueryRequestAction> Handle(AddRequestAction request, CancellationToken cancellationToken)
            {
                var newTask = new DbRequestAction
                {
                    Title = request.Title,
                    Body = request.Body,
                    Type = request.Type,
                    SubType = request.SubType,
                    Source = request.Source.MapToDatabase(),
                    Responsible = request.Responsible.MapToDatabase(),
                    PropertiesJson = request.Properties?.SerializeToStringOrDefault(),

                    RequestId = request.RequestId,
                };

                db.RequestTasks.Add(newTask);
                await db.SaveChangesAsync(cancellationToken);

                return new QueryRequestAction(newTask);
            }
        }
    }
}
