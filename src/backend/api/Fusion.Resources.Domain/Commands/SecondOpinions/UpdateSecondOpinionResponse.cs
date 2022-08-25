using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    public class UpdateSecondOpinionResponse : IRequest<QuerySecondOpinionResponse?>
    {
        private Guid secondOpinionId;
        private Guid responseId;

        public UpdateSecondOpinionResponse(Guid secondOpinionId, Guid responseId)
        {
            this.secondOpinionId = secondOpinionId;
            this.responseId = responseId;
        }

        public MonitorableProperty<string> Comment { get; set; } = new();
        public MonitorableProperty<QuerySecondOpinionResponseStates> State { get; set; } = new();


        public class Handler : IRequestHandler<UpdateSecondOpinionResponse, QuerySecondOpinionResponse?>
        {
            private readonly ResourcesDbContext db;
            private readonly IMediator mediator;

            public Handler(ResourcesDbContext db, IMediator mediator)
            {
                this.db = db;
                this.mediator = mediator;
            }

            public async Task<QuerySecondOpinionResponse?> Handle(UpdateSecondOpinionResponse request, CancellationToken cancellationToken)
            {
                var secondOpinion = await db.SecondOpinions
                    .Include(x => x.Responses!).ThenInclude(x => x.AssignedTo)
                    .Include(x => x.CreatedBy)
                    .FirstOrDefaultAsync(x => x.Id == request.secondOpinionId, cancellationToken);

                var response = secondOpinion?.Responses?.FirstOrDefault(x => x.Id == request.responseId);
                if (response == null) return null;

                bool wasPublished = false;
                request.State.IfSet(x =>
                {
                    var dbState = x switch
                    {
                        QuerySecondOpinionResponseStates.Open => DbSecondOpinionResponseStates.Open,
                        QuerySecondOpinionResponseStates.Draft => DbSecondOpinionResponseStates.Draft,
                        QuerySecondOpinionResponseStates.Published => DbSecondOpinionResponseStates.Published,
                        _ => throw new NotImplementedException()
                    };

                    response.State = dbState;
                    wasPublished = dbState == DbSecondOpinionResponseStates.Published;
                });


                if (wasPublished)
                {
                    response.AnsweredAt = DateTimeOffset.Now;
                }

                request.Comment.IfSet(x => response.Comment = x);

                var wasSaved = await db.SaveChangesAsync(cancellationToken) > 0;

                var model = new QuerySecondOpinionResponse(response);
                if (wasPublished && wasSaved)
                {
                    await mediator.Publish(new SecondOpinionAnswered(new QuerySecondOpinion(secondOpinion), model), cancellationToken);
                }

                return model;
            }
        }
    }
}
