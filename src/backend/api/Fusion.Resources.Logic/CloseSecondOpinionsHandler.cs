using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Logic.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Logic
{
    internal class CloseSecondOpinionsHandler : INotificationHandler<RequestProvisioned>
    {
        private readonly ResourcesDbContext db;

        public CloseSecondOpinionsHandler(ResourcesDbContext db)
        {
            this.db = db;
        }

        public async Task Handle(RequestProvisioned notification, CancellationToken ct)
        {
            var responses = await db.SecondOpinionResponses
                .Where(x => x.SecondOpinion.RequestId == notification.RequestId)
                .ToListAsync(ct);

            foreach (var response in responses)
            {
                if(response.State != DbSecondOpinionResponseStates.Published)
                    response.State = DbSecondOpinionResponseStates.Closed;
                
                response.Comment = "Comments are hidden when request is closed.";
            }

            await db.SaveChangesAsync(ct);
        }
    }
}
