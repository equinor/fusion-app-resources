using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    public class DeleteComment : IRequest
    {
        public DeleteComment(Guid commentId)
        {
            CommentId = commentId;
        }

        public Guid CommentId { get; }

        public class Handler : AsyncRequestHandler<DeleteComment>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            protected override async Task Handle(DeleteComment command, CancellationToken cancellationToken)
            {
                var comment = await db.RequestComments.FirstOrDefaultAsync(c => c.Id == command.CommentId);

                if (comment == null)
                    throw new InvalidOperationException($"Comment with id '{command.CommentId}' was not found");

                db.RequestComments.Remove(comment);
                await db.SaveChangesAsync();
            }
        }
    }
}
