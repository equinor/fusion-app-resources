using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class DeleteSecondOpinion : IRequest<bool>
    {
        public DeleteSecondOpinion(Guid secondOpinionId)
        {
            SecondOpinionId = secondOpinionId;
        }

        public Guid SecondOpinionId { get; }

        public class Handler : IRequestHandler<DeleteSecondOpinion, bool>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            public async Task<bool> Handle(DeleteSecondOpinion request, CancellationToken ct)
            {
                var secondOpinion = await db.SecondOpinions
                    .Include(x => x.Responses)
                    .SingleOrDefaultAsync(x => x.Id == request.SecondOpinionId,  ct);
                
                if (secondOpinion is null) return false;

                db.RemoveRange(secondOpinion.Responses!);
                db.Remove(secondOpinion);

                return await db.SaveChangesAsync(ct) > 0;
            }
        }
    }
}
