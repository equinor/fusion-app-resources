using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class DeleteSecondOpinionResponse : IRequest<bool>
    {
        public Guid SecondOpinionId { get; }
        public Guid ResponseId { get; }

        public DeleteSecondOpinionResponse(Guid secondOpinionId, Guid responseId)
        {
            SecondOpinionId = secondOpinionId;
            ResponseId = responseId;
        }

        public class Handler : IRequestHandler<DeleteSecondOpinionResponse, bool>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            public async Task<bool> Handle(DeleteSecondOpinionResponse request, CancellationToken cancellationToken)
            {
                var response = db.SecondOpinionResponses.SingleOrDefaultAsync(x => x.Id == request.ResponseId);
                if (response is null) return false;

                db.Remove(response);

                return await db.SaveChangesAsync(cancellationToken) > 0;
            }
        }
    }
}
