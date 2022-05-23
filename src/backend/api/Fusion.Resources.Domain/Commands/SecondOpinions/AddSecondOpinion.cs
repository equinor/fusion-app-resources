using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain.Commands.Requests.Sharing;
using Fusion.Resources.Domain.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    public class AddSecondOpinion : TrackableRequest<QuerySecondOpinion>
    {
        private Guid requestId;
        private string description;
        private List<PersonId> assignedToIds;

        public AddSecondOpinion(Guid requestId, string description, IEnumerable<PersonId> assignedToIds)
        {
            this.requestId = requestId;
            this.description = description;
            this.assignedToIds = assignedToIds.ToList();
        }

        public class Handler : IRequestHandler<AddSecondOpinion, QuerySecondOpinion>
        {
            private readonly ResourcesDbContext db;
            private readonly IMediator mediator;
            private readonly IProfileService profileService;

            public Handler(ResourcesDbContext db, IMediator mediator, IProfileService profileService)
            {
                this.db = db;
                this.mediator = mediator;
                this.profileService = profileService;
            }

            public async Task<QuerySecondOpinion> Handle(AddSecondOpinion request, CancellationToken cancellationToken)
            {
                var secondOpinion = new DbSecondOpinionPrompt
                {
                    Description = request.description
                };

                foreach (var personId in request.assignedToIds)
                {
                    var person = await profileService.EnsurePersonAsync(personId);
                    if (person is null) continue;

                    secondOpinion.Responses.Add(new DbSecondOpinionResponse
                    {
                        AssignedTo = person,
                        State = DbSecondOpinionResponseStates.Open
                    });
                }

                var shareCommand = new ShareRequest(
                    request.requestId, 
                    SharedRequestScopes.BasicRead, 
                    SharedRequestSource.SecondOpinion, 
                    $"Request shared by {request.Editor.Person.Name} for second opinion."
                );
                shareCommand.SharedWith.AddRange(request.assignedToIds);

                await mediator.Send(shareCommand, cancellationToken);


                db.SecondOpinions.Add(secondOpinion);
                await db.SaveChangesAsync(cancellationToken);

                return new QuerySecondOpinion(secondOpinion);
            }
        }
    }
}
