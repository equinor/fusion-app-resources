using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    public class AddRequestAction : IRequest<QueryRequestAction>
    {
        public AddRequestAction(Guid requestId, string title, string? body, string type)
        {
            RequestId = requestId;
            Title = title;
            Body = body;
            Type = type;
        }

        public Guid RequestId { get; }
        public string Title { get; }
        public string? Body { get; set; }
        public string Type { get; set; }
        public string? SubType { get; set; }
        public QueryTaskSource Source { get; set; }
        public QueryTaskResponsible Responsible { get; set; }
        public Dictionary<string, object>? Properties { get; set; }
        public bool IsRequired { get; set; }
        public Guid? AssignedToId { get; set; }
        public DateTime? DueDate { get; set; }

        public class Handler : IRequestHandler<AddRequestAction, QueryRequestAction>
        {
            private readonly ResourcesDbContext db;
            private readonly IProfileService profileService;
            private readonly IHttpContextAccessor context;

            public Handler(ResourcesDbContext db, IProfileService profileService, IHttpContextAccessor context)
            {
                this.db = db;
                this.profileService = profileService;
                this.context = context;
            }

            public async Task<QueryRequestAction> Handle(AddRequestAction request, CancellationToken cancellationToken)
            {
                var userId = context.HttpContext!.User.GetAzureUniqueIdOrThrow();
                var creator = await profileService.EnsurePersonAsync(userId);

                DbPerson? assignedTo = null;
                if (request.AssignedToId.HasValue)
                    assignedTo = await profileService.EnsurePersonAsync(request.AssignedToId.Value);

                var newTask = new DbRequestAction
                {
                    Title = request.Title,
                    Body = request.Body,
                    Type = request.Type,
                    SubType = request.SubType,
                    Source = request.Source.MapToDatabase(),
                    Responsible = request.Responsible.MapToDatabase(),
                    PropertiesJson = request.Properties?.SerializeToStringOrDefault(),
                    SentById = creator!.Id,
                    RequestId = request.RequestId,
                    IsRequired = request.IsRequired,
                    AssignedToId = assignedTo?.Id,
                    DueDate = request.DueDate
                };

                db.RequestActions.Add(newTask);
                await db.SaveChangesAsync(cancellationToken);

                return new QueryRequestAction(newTask);
            }
        }
    }
}
