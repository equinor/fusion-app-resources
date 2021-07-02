using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    public class AddRequestTask : IRequest<QueryRequestTask>
    {
        public AddRequestTask(Guid requestId, string title, string body, string category, string type) 
        {
            RequestId = requestId;
            Title = title;
            Body = body;
            Category = category;
            Type = type;
        }

        public Guid RequestId { get; }
        public string Title { get; }
        public string Body { get; set; }
        public string Category { get; set; }
        public string Type { get; set; }
        public string? SubType { get; set; }
        public TaskSource Source { get; set; }
        public TaskResponsible Responsible { get; set; }
        public Dictionary<string, object>? Properties { get; set; }

        public class Handler : IRequestHandler<AddRequestTask, QueryRequestTask>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            public async Task<QueryRequestTask> Handle(AddRequestTask request, CancellationToken cancellationToken)
            {
                var newTask = new DbRequestTask
                {
                    Title = request.Title,
                    Body = request.Body,
                    Category = request.Category,
                    Type = request.Type,
                    SubType = request.SubType,
                    Source = request.Source.MapToDatabase(),
                    Responsible = request.Responsible.MapToDatabase(),
                    PropertiesJson = JsonSerializer.Serialize(request.Properties),

                    RequestId = request.RequestId,
                };

                db.RequestTasks.Add(newTask);
                await db.SaveChangesAsync(cancellationToken);

                return new QueryRequestTask(newTask);
            }
        }
    }
}
