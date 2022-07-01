using Fusion.Integration.LineOrg;
using Fusion.Integration.Org;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    public class UpdateResponsibilityMatrix : TrackableRequest<QueryResponsibilityMatrix>
    {
        public UpdateResponsibilityMatrix(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; set; }
        public MonitorableProperty<Guid?> ProjectId { get; set; } = new MonitorableProperty<Guid?>();
        public MonitorableProperty<Guid?> LocationId { get; set; } = new MonitorableProperty<Guid?>();
        public MonitorableProperty<string?> Discipline { get; set; } = new MonitorableProperty<string?>();
        public MonitorableProperty<Guid?> BasePositionId { get; set; } = new MonitorableProperty<Guid?>();
        public MonitorableProperty<string?> Sector { get; set; } = new MonitorableProperty<string?>();
        public MonitorableProperty<string?> Unit { get; set; } = new MonitorableProperty<string?>();

        public class Handler : IRequestHandler<UpdateResponsibilityMatrix, QueryResponsibilityMatrix>
        {
            private readonly ResourcesDbContext resourcesDb;
            private readonly IMediator mediator;
            private readonly IProfileService profileService;
            private readonly IProjectOrgResolver orgResolver;
            private readonly ILineOrgResolver lineOrgResolver;

            public Handler(ResourcesDbContext resourcesDb, IMediator mediator, IProfileService profileService, IProjectOrgResolver orgResolver, ILineOrgResolver lineOrgResolver)
            {
                this.resourcesDb = resourcesDb;
                this.mediator = mediator;
                this.profileService = profileService;
                this.orgResolver = orgResolver;
                this.lineOrgResolver = lineOrgResolver;
            }

            public async Task<QueryResponsibilityMatrix> Handle(UpdateResponsibilityMatrix request, CancellationToken cancellationToken)
            {
                bool isModified = false;
                var status = await resourcesDb.ResponsibilityMatrices
                    .Include(cp => cp.CreatedBy)
                    .Include(cp => cp.Project)
                    .Include(cp => cp.Responsible)
                    .FirstOrDefaultAsync(x => x.Id == request.Id);

                if (status is null)
                    throw new ArgumentException($"Cannot locate status using identifier '{request.Id}'");

                if (request.ProjectId.HasBeenSet)
                {
                    if (request.ProjectId.Value != null)
                    {
                        var project = await EnsureProjectAsync(request.ProjectId.Value.Value);
                        if (project == null)
                            throw new ArgumentException("Unable to resolve project using org service");

                        status.Project = project;
                    }
                    else
                    {
                        status.Project = null;
                    }

                    isModified = true;
                }

                if (request.LocationId.HasBeenSet)
                {
                    status.LocationId = request.LocationId.Value;
                    isModified = true;
                }
                if (request.Discipline.HasBeenSet)
                {
                    status.Discipline = request.Discipline.Value;
                    isModified = true;
                }
                if (request.BasePositionId.HasBeenSet)
                {
                    status.BasePositionId = request.BasePositionId.Value;
                    isModified = true;
                }
                if (request.Sector.HasBeenSet)
                {
                    status.Sector = request.Sector.Value;
                    isModified = true;
                }

                if (request.Unit.HasBeenSet)
                {
                    status.Unit = request.Unit.Value;
                    isModified = true;

                    status.Responsible = await GetResourceOwner(request.Unit.Value!);
                }

                if (isModified)
                {
                    status.UpdatedBy = request.Editor.Person;
                    status.Updated = DateTimeOffset.UtcNow;
                    await resourcesDb.SaveChangesAsync();
                }

                var returnItem = await mediator.Send(new GetResponsibilityMatrixItem(request.Id));
                return returnItem!;
            }

            private async Task<DbPerson?> GetResourceOwner(string departmentId)
            {
                var department = await lineOrgResolver.ResolveDepartmentAsync(DepartmentId.FromFullPath(departmentId));
                if(department?.Manager?.AzureUniqueId is not null)
                {
                    var azureUniqueId = department.Manager.AzureUniqueId;
                    return await profileService.EnsurePersonAsync(new PersonId(azureUniqueId));
                }
                return null;
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
