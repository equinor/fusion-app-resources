using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Queries
{
    public class GetRequestComment : IRequest<QueryRequestComment>
    {
        public GetRequestComment(Guid commentId)
        {
            CommentId = commentId;
        }

        public Guid CommentId { get; }

        public class Handler : IRequestHandler<GetRequestComment, QueryRequestComment>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            public async Task<QueryRequestComment?> Handle(GetRequestComment request, CancellationToken cancellationToken)
            {
                var comment = await db.RequestComments
                    .Include(c => c.CreatedBy)
                    .Include(c => c.UpdatedBy)
                    .FirstOrDefaultAsync(c => c.Id == request.CommentId);

                if (comment == null)
                    return null;

                return new QueryRequestComment(comment);
            }
        }
    }
}
