using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class GetSecondOpinions : IRequest<List<QuerySecondOpinion>>
    {
        public PersonId? CreatedById { get; private set; }
        public PersonId? AssigneeId { get; private set; }
        public Guid? RequestId { get; private set; }
        public Guid? Id { get; private set; }

        public GetSecondOpinions WithCreator(PersonId creator)
        {
            CreatedById = creator;
            return this;
        }

        public GetSecondOpinions WithRequest(Guid requestId)
        {
            RequestId = requestId;
            return this;
        }

        public GetSecondOpinions WithAssignee(PersonId assigneeId)
        {
            AssigneeId = assigneeId;
            return this;
        }


        public GetSecondOpinions WithId(Guid secondOpinionId)
        {
            Id = secondOpinionId;
            return this;
        }

        public class Handler : IRequestHandler<GetSecondOpinions, List<QuerySecondOpinion>>
        {
            private readonly ResourcesDbContext db;
            private readonly IProfileService profileService;

            public Handler(ResourcesDbContext db, IProfileService profileService)
            {
                this.db = db;
                this.profileService = profileService;
            }

            public async Task<List<QuerySecondOpinion>> Handle(GetSecondOpinions request, CancellationToken cancellationToken)
            {
                var creatorId = default(Guid?);
                var assigneeId = default(Guid?);

                if (request.CreatedById.HasValue)
                {
                    var creator = await profileService.EnsurePersonAsync(request.CreatedById.Value);
                    creatorId = creator?.Id;
                }

                if (request.AssigneeId.HasValue)
                {
                    var assignee = await profileService.EnsurePersonAsync(request.AssigneeId.Value);
                    assigneeId = assignee?.Id;
                }

                var query = db.SecondOpinions
                    .Include(x => x.Responses.Where(r => r.AssignedToId == assigneeId || assigneeId == null))
                        .ThenInclude(x => x.AssignedTo)
                    .Include(x => x.CreatedBy)
                    .AsQueryable();

                if (request.Id.HasValue)
                {
                    query = query.Where(r => r.Id == request.Id);
                }

                if (creatorId.HasValue)
                {
                    query = query.Where(x => x.CreatedById == creatorId);
                }

                if (assigneeId.HasValue)
                { 
                    query = query.Where(x => x.Responses.Any(x => x.AssignedToId == assigneeId));
                }

                if (request.RequestId.HasValue)
                {
                    query = query.Where(x => x.RequestId == request.RequestId);
                }

                var secondOpinions = await query.ToListAsync(cancellationToken);

                return secondOpinions.Select(x => new QuerySecondOpinion(x)).ToList();
            }
        }
    }
}
