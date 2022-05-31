using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    public class UpdateSecondOpinion : IRequest<QuerySecondOpinion?>
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
            private readonly IProfileService profileService;

            public Handler(ResourcesDbContext db, IProfileService profileService)
            {
                this.db = db;
                this.profileService = profileService;
            }

            public async Task<QuerySecondOpinion?> Handle(UpdateSecondOpinion request, CancellationToken ct)
            {
                var entity = await db.SecondOpinions
                    .Include(x => x.Responses)
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
                    
                    entity.Responses.RemoveAll(x => !assigneeIds.Contains(x.AssignedToId));
                    var addedAssignees = assigneeIds.Except(entity.Responses.Select(x => x.AssignedToId));

                    foreach (var assigneeId in addedAssignees)
                    {
                        entity.Responses.Add(new DbSecondOpinionResponse
                        {
                            PromptId = request.SecondOpinionId,
                            AssignedToId = assigneeId,
                            State = DbSecondOpinionResponseStates.Open
                        });
                    }
                }

                await db.SaveChangesAsync(ct);
                return new QuerySecondOpinion(entity);
            }
        }
    }
}
