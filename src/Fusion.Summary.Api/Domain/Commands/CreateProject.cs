using Fusion.Summary.Api.Controllers.Requests;
using Fusion.Summary.Api.Database;
using Fusion.Summary.Api.Database.Models;
using Fusion.Summary.Api.Domain.Models;
using MediatR;

namespace Fusion.Summary.Api.Domain.Commands;

public class CreateProject : IRequest<QueryProject>
{
    public string Name { get; }
    public Guid OrgProjectExternalId { get; }
    public Guid? DirectorAzureUniqueId { get; }
    public List<Guid> AssignedAdminsAzureUniqueId { get; }

    public CreateProject(PutProjectRequest putRequest)
    {
        Name = putRequest.Name;
        OrgProjectExternalId = putRequest.OrgProjectExternalId;
        DirectorAzureUniqueId = putRequest.DirectorAzureUniqueId;
        AssignedAdminsAzureUniqueId = putRequest.AssignedAdminsAzureUniqueId.ToList();
    }

    public class Handler : IRequestHandler<CreateProject, QueryProject>
    {
        private readonly SummaryDbContext _dbContext;

        public Handler(SummaryDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<QueryProject> Handle(CreateProject request, CancellationToken cancellationToken)
        {
            var dbProject = new DbProject()
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                OrgProjectExternalId = request.OrgProjectExternalId,
                DirectorAzureUniqueId = request.DirectorAzureUniqueId,
                AssignedAdminsAzureUniqueId = request.AssignedAdminsAzureUniqueId
            };

            _dbContext.Projects.Add(dbProject);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return QueryProject.FromDbProject(dbProject);
        }
    }
}