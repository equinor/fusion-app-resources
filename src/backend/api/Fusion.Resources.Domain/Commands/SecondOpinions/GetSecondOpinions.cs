using Fusion.AspNetCore.OData;
using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class GetSecondOpinions : IRequest<QuerySecondOpinionResult>
    {
        public PersonId? CreatedById { get; private set; }
        public PersonId? AssigneeId { get; private set; }
        public Guid? RequestId { get; private set; }
        public Guid? Id { get; private set; }
        public ODataQueryParams Query { get; private set; } = new ODataQueryParams();
        public bool CountOnly { get; private set; }

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


        public GetSecondOpinions WithQuery(ODataQueryParams query)
        {
            Query = query;
            return this;
        }

        public GetSecondOpinions WithCountOnly(bool countEnabled = true)
        {
            CountOnly = countEnabled;
            return this;
        }

        public class Handler : IRequestHandler<GetSecondOpinions, QuerySecondOpinionResult>
        {
            private readonly ResourcesDbContext db;
            private readonly IProfileService profileService;

            public Handler(ResourcesDbContext db, IProfileService profileService)
            {
                this.db = db;
                this.profileService = profileService;
            }

            public async Task<QuerySecondOpinionResult> Handle(GetSecondOpinions request, CancellationToken cancellationToken)
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
                    .Include(x => x.Responses!).ThenInclude(x => x.AssignedTo)
                    .Include(x => x.CreatedBy)
                    .Include(x => x.Request).ThenInclude(x => x.Project)
                    .Include(x => x.Request).ThenInclude(x => x.CreatedBy)
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
                    query = query.Where(x => x.Responses!.Any(x => x.AssignedToId == assigneeId));
                }

                if (request.RequestId.HasValue)
                {
                    query = query.Where(x => x.RequestId == request.RequestId);
                }

                if(request.Query.HasFilter)
                {
                    var filter = request.Query.Filter.GetFilterForField("state");
                    if (filter.Value.Equals("Active", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(x => x.Request.State.State != "completed");
                    }
                    else if(filter.Value.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(x => x.Request.State.State == "completed");
                    }
                }

                var secondOpinions = await query.ToListAsync(cancellationToken);

                return request.CountOnly 
                    ? QuerySecondOpinionResult.CreateCountOnly(secondOpinions, assigneeId)
                    : QuerySecondOpinionResult.Create(secondOpinions, assigneeId);
            }
        }
    }
}
