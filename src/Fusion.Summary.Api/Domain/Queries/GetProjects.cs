using Fusion.Summary.Api.Database;
using Fusion.Summary.Api.Domain.Models;
using Fusion.Summary.Api.Domain.Queries.Base;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Summary.Api.Domain.Queries;

public class GetProjects : IRequest<QueryCollection<QueryProject>>
{
    public Guid? ProjectId { get; private set; }

    public GetProjects WhereProjectId(Guid projectId)
    {
        ProjectId = projectId;
        return this;
    }


    public class Handler : IRequestHandler<GetProjects, QueryCollection<QueryProject>>
    {
        private readonly SummaryDbContext _dbContext;

        public Handler(SummaryDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<QueryCollection<QueryProject>> Handle(GetProjects request, CancellationToken cancellationToken)
        {
            var query = _dbContext.Projects.AsQueryable();

            if (request.ProjectId.HasValue)
                query = query.Where(p => p.Id == request.ProjectId || p.OrgProjectExternalId == request.ProjectId);

            var projects = await query.ToListAsync(cancellationToken);

            return new QueryCollection<QueryProject>(projects.Select(QueryProject.FromDbProject));
        }
    }
}