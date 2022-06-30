using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain.Commands.Requests.Sharing;
using Fusion.Resources.Domain.Models;
using Fusion.Resources.Domain.Queries;
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

        public MonitorableProperty<string> Title { get; set; } = new();
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


                var requestItem = await mediator.Send(new GetResourceAllocationRequestItem(entity.RequestId), ct);
                if (requestItem == null) return null;

                request.Title.IfSet(x => entity.Title = x);
                request.Description.IfSet(x => entity.Description = x);

                if (request.AssignedTo.HasBeenSet)
                {
                    var assignees = await profileService.EnsurePersonsAsync(request.AssignedTo.Value);
                    var assigneeIds = assignees
                        .Select(x => x.Id)
                        .ToHashSet();

                    var toRemove = entity.Responses!.Where(x => !assigneeIds.Contains(x.AssignedToId)).ToArray();
                    foreach (var response in toRemove)
                    {
                        db.SecondOpinionResponses.Remove(response);
                        await mediator.Send(new RevokeShareRequest(entity.RequestId, new PersonId(response.AssignedTo.AzureUniqueId), SharedRequestSource.SecondOpinion), ct);
                    }

                    var addedAssigneeIds = assigneeIds.Except(entity.Responses!.Select(x => x.AssignedToId));
                    var addedAssignees = assignees
                        .Where(x => addedAssigneeIds.Contains(x.Id))
                        .ToList();
                    foreach (var assignee in addedAssignees)
                    {
                        entity.Responses!.Add(new DbSecondOpinionResponse
                        {
                            PromptId = request.SecondOpinionId,
                            AssignedToId = assignee.Id,
                            State = DbSecondOpinionResponseStates.Open
                        });
                        await mediator.Publish(new SecondOpinionRequested(new QuerySecondOpinion(entity), requestItem, new QueryPerson(assignee)), ct);
                    }

                    var shareCommand = new ShareRequest(
                        entity.RequestId,
                        SharedRequestScopes.BasicRead,
                        SharedRequestSource.SecondOpinion,
                        $"Request shared by {request.Editor.Person.Name} for second opinion."
                    );

                    shareCommand.SharedWith.AddRange(addedAssignees.Select(x => new PersonId(x.AzureUniqueId)));
                    await mediator.Send(shareCommand, ct);
                }

                await db.SaveChangesAsync(ct);
                return new QuerySecondOpinion(entity);
            }
        }
    }
}
