using Fusion.Resources.Database;
using MediatR;
using System;
using System.Linq;
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
                db.RequestComments.RemoveRange(
                    db.RequestComments.Where(c => c.Id == command.CommentId)
                );
                
                if(await db.SaveChangesAsync(cancellationToken) <= 0)
                {
                    throw new InvalidOperationException($"Comment with id '{command.CommentId}' was not found");
                }
            }
        }
    }
}
