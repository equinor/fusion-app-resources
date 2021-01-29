using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Integration.Org;
using Fusion.Resources.Database.Entities;

namespace Fusion.Resources.Domain.Commands
{
    public class UpdateResponsibilityMatrix : TrackableRequest<QueryResponsibilityMatrix>
    {
        public UpdateResponsibilityMatrix(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public Guid LocationId { get; set; }
        public string? Discipline { get; set; }
        public Guid BasePositionId { get; set; }
        public string? Sector { get; set; }
        public string? Unit { get; set; }
        public Guid? ResponsibleId { get; set; }

        public class Handler : IRequestHandler<UpdateResponsibilityMatrix, QueryResponsibilityMatrix>
        {
            private readonly ResourcesDbContext resourcesDb;
            private readonly IMediator mediator;
            private readonly IProfileService profileService;
            private readonly IProjectOrgResolver orgResolver;

            public Handler(ResourcesDbContext resourcesDb, IMediator mediator, IProfileService profileService, IProjectOrgResolver orgResolver)
            {
                this.resourcesDb = resourcesDb;
                this.mediator = mediator;
                this.profileService = profileService;
                this.orgResolver = orgResolver;
            }

            public async Task<QueryResponsibilityMatrix> Handle(UpdateResponsibilityMatrix request, CancellationToken cancellationToken)
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

                var status = await resourcesDb.ResponsibilityMatrices
                    .Include(cp => cp.CreatedBy)
                    .Include(cp => cp.Project)
                    .Include(cp => cp.Responsible)
                    .FirstOrDefaultAsync(x => x.Id == request.Id);

                if (status is null)
                    throw new ArgumentException($"Cannot locate status using identifier '{request.Id}'");

                //status.UpdatedBy = request.Editor.Person;
                //status.Updated = DateTimeOffset.UtcNow;
                status.Project = project;
                status.LocationId = request.LocationId;
                status.Discipline = request.Discipline;
                status.BasePositionId = request.BasePositionId;
                status.Sector = request.Sector;
                status.Unit = request.Unit;
                status.Responsible = responsible;


                await resourcesDb.SaveChangesAsync();

                var returnItem = await mediator.Send(new GetResponsibilityMatrixItem(request.Id));
                return returnItem!;
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
