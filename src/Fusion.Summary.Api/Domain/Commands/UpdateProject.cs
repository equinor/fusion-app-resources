using Fusion.Summary.Api.Controllers.Requests;
using Fusion.Summary.Api.Database;
using Fusion.Summary.Api.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Summary.Api.Domain.Commands;

public class UpdateProject : IRequest<QueryProject>
{
    public Guid Id { get; }
    public string Name { get; }
    public Guid OrgProjectExternalId { get; }
    public Guid? DirectorAzureUniqueId { get; }
    public List<Guid> AssignedAdminsAzureUniqueId { get; }

    public UpdateProject(Guid id, PutProjectRequest putRequest)
    {
        Id = id;
        Name = putRequest.Name;
        OrgProjectExternalId = putRequest.OrgProjectExternalId;
        DirectorAzureUniqueId = putRequest.DirectorAzureUniqueId;
        AssignedAdminsAzureUniqueId = putRequest.AssignedAdminsAzureUniqueId.ToList();
    }


    public class Handler : IRequestHandler<UpdateProject, QueryProject>
    {
        private readonly SummaryDbContext _dbContext;

        public Handler(SummaryDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<QueryProject> Handle(UpdateProject request, CancellationToken cancellationToken)
        {
            var project = await _dbContext.Projects.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
            if (project == null)
                throw new InvalidOperation($"Project with id {request.OrgProjectExternalId} not found");

            project.Name = request.Name;
            project.OrgProjectExternalId = request.OrgProjectExternalId;
            project.DirectorAzureUniqueId = request.DirectorAzureUniqueId;
            project.AssignedAdminsAzureUniqueId = request.AssignedAdminsAzureUniqueId;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return QueryProject.FromDbProject(project);
        }
    }
}