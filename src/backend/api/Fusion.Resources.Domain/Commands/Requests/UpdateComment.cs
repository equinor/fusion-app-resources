using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    public class UpdateComment : TrackableRequest
    {
        public UpdateComment(Guid commentId, string content)
        {
            CommentId = commentId;
            Content = content;
        }

        public Guid CommentId { get; }
        public string Content { get; }

        public class Handler : AsyncRequestHandler<UpdateComment>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            protected override async Task Handle(UpdateComment request, CancellationToken cancellationToken)
            {
                var comment = await db.RequestComments.FirstOrDefaultAsync(c => c.Id == request.CommentId);

                if (comment == null)
                    throw new InvalidOperationException($"Comment with id '{request.CommentId}' was not found");

                comment.Comment = request.Content;
                comment.Updated = DateTimeOffset.UtcNow;
                comment.UpdatedById = request.Editor.Person.Id;

                await db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
