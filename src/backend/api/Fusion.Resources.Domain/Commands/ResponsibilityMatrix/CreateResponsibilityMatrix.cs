using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Integration.Org;
using Microsoft.EntityFrameworkCore;

#nullable enable 

namespace Fusion.Resources.Domain.Commands
{

    public class CreateResponsibilityMatrix : TrackableRequest<QueryResponsibilityMatrix>
    {
        public Guid ProjectId { get; set; }
        public Guid LocationId { get; set; }
        public string? Discipline { get; set; }
        public Guid BasePositionId { get; set; }
        public string? Sector { get; set; }
        public string? Unit { get; set; }
        public Guid? ResponsibleId { get; set; }

        public class Handler : IRequestHandler<CreateResponsibilityMatrix, QueryResponsibilityMatrix>
        {
            private readonly ResourcesDbContext resourcesDb;
            private readonly IProfileService profileService;
            private readonly IProjectOrgResolver orgResolver;

            public Handler(ResourcesDbContext resourcesDb, IProfileService profileService, IProjectOrgResolver orgResolver)
            {
                this.resourcesDb = resourcesDb;
                this.profileService = profileService;
                this.orgResolver = orgResolver;
            }

            public async Task<QueryResponsibilityMatrix> Handle(CreateResponsibilityMatrix request, CancellationToken cancellationToken)
            {
                var project = await EnsureProjectAsync(request.ProjectId);
                if (project == null)
                {
                    throw new ArgumentException("Unable to resolve project using org service");
                }
                DbPerson? responsible = null;
                if (request.ResponsibleId != null)
                    responsible = await profileService.EnsurePersonAsync(request.ResponsibleId.Value);
                if (responsible == null)
                    throw new ArgumentException("Cannot create personnel without either a valid azure unique id or mail address");

                var newItem = new DbResponsibilityMatrix
                {
                    Id = Guid.NewGuid(),
                    CreatedBy = request.Editor.Person,
                    Created = DateTimeOffset.UtcNow,
                    Project = project,
                    LocationId = request.LocationId,
                    Discipline = request.Discipline,
                    BasePositionId = request.BasePositionId,
                    Sector = request.Sector,
                    Unit = request.Unit,
                    Responsible = responsible
                };

                await resourcesDb.ResponsibilityMatrices.AddAsync(newItem);
                await resourcesDb.SaveChangesAsync();

                return new QueryResponsibilityMatrix(newItem);
            }

            private async Task<DbProject?> EnsureProjectAsync(Guid projectId)
            {
                var orgProject = await orgResolver.ResolveProjectAsync(projectId);
                if (orgProject == null)
                    return null;

                var project = await resourcesDb.Projects.FirstOrDefaultAsync(x => x.OrgProjectId == projectId) ?? new DbProject
                {
                    Id = Guid.NewGuid(),
                    Name = orgProject.Name,
                    OrgProjectId = orgProject.ProjectId,
                    DomainId = orgProject.DomainId
                };
                return project;
            }
        }
    }
}
