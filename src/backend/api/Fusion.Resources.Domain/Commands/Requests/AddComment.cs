using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    public class AddComment : TrackableRequest<QueryRequestComment>
    {
        public AddComment(QueryRequestOrigin origin, Guid requestId, string comment)
        {
            Origin = origin;
            RequestId = requestId;
            Comment = comment;
        }

        public Guid RequestId { get; }
        public QueryRequestOrigin Origin { get; }

        public string Comment { get; }

        public class Handler : IRequestHandler<AddComment, QueryRequestComment>
        {
            private readonly ResourcesDbContext db;
            private readonly IMediator mediator;

            public Handler(ResourcesDbContext db, IMediator mediator)
            {
                this.db = db;
                this.mediator = mediator;
            }

            public async Task<QueryRequestComment> Handle(AddComment command, CancellationToken cancellationToken)
            {
                Enum.TryParse<DbRequestComment.DbOrigin>($"{command.Origin}", out var dbOrigin);
                
                var comment = new DbRequestComment
                {
                    Id = Guid.NewGuid(),
                    Comment = command.Comment,
                    Origin = dbOrigin,
                    Created = DateTimeOffset.UtcNow,
                    CreatedById = command.Editor.Person.Id,
                    RequestId = command.RequestId
                };

                await db.RequestComments.AddAsync(comment);
                await db.SaveChangesAsync();

                await mediator.Publish(new Notifications.CommentAdded(comment.Id));

                return new QueryRequestComment(comment);
            }
        }
    }
}
