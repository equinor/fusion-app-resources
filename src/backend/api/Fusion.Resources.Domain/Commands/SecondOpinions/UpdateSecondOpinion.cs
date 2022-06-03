using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain.Commands.Requests.Sharing;
using Fusion.Resources.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    public class UpdateSecondOpinion : TrackableRequest<QuerySecondOpinion?>
    {
        public UpdateSecondOpinion(Guid secondOpinionId)
        {
            SecondOpinionId = secondOpinionId;
        }

        public Guid SecondOpinionId { get; }

        public MonitorableProperty<string> Description { get; set; } = new();
        public MonitorableProperty<List<PersonId>> AssignedTo { get; set; } = new();

        public class Handler : IRequestHandler<UpdateSecondOpinion, QuerySecondOpinion?>
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

            public async Task<QuerySecondOpinion?> Handle(UpdateSecondOpinion request, CancellationToken ct)
            {
                var entity = await db.SecondOpinions
                    .Include(x => x.Responses).ThenInclude(x => x.AssignedTo)
                    .Include(x => x.CreatedBy)
                    .FirstOrDefaultAsync(x => x.Id == request.SecondOpinionId, ct);

                if (entity == null) return null;

                request.Description.IfSet(x => entity.Description = x);

                if (request.AssignedTo.HasBeenSet)
                {
                    var assignees = await profileService.EnsurePersonsAsync(request.AssignedTo.Value);
                    var assigneeIds = assignees
                        .Select(x => x.Id)
                        .ToHashSet();

                    var toRemove = entity.Responses.Where(x => !assigneeIds.Contains(x.AssignedToId)).ToArray();
                    foreach (var response in toRemove)
                    {
                        db.SecondOpinionResponses.Remove(response);
                        await mediator.Send(new RevokeShareRequest(entity.RequestId, new PersonId(response.AssignedTo.AzureUniqueId), SharedRequestSource.SecondOpinion), ct);
                    }

                    var addedAssignees = assigneeIds.Except(entity.Responses.Select(x => x.AssignedToId)).ToList();
                    foreach (var assigneeId in addedAssignees)
                    {
                        entity.Responses.Add(new DbSecondOpinionResponse
                        {
                            PromptId = request.SecondOpinionId,
                            AssignedToId = assigneeId,
                            State = DbSecondOpinionResponseStates.Open
                        });
                    }

                    var shareCommand = new ShareRequest(
                        entity.RequestId,
                        SharedRequestScopes.BasicRead,
                        SharedRequestSource.SecondOpinion,
                        $"Request shared by {request.Editor.Person.Name} for second opinion."
                    );

                    //TODO: Filter once
                    var addedAzureIds = assignees.Where(x => assigneeIds.Contains(x.Id)).Select(x => x.AzureUniqueId).ToArray();
                    shareCommand.SharedWith.AddRange(addedAzureIds.Select(x => new PersonId(x)));
                    await mediator.Send(shareCommand, ct);
                }

                await db.SaveChangesAsync(ct);
                return new QuerySecondOpinion(entity);
            }
        }
    }
}
