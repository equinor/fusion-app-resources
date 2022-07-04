using Fusion.Integration.LineOrg;
using Fusion.Integration.Org;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Fusion.Resources.Domain.Commands
{

    public class CreateResponsibilityMatrix : TrackableRequest<QueryResponsibilityMatrix>
    {
        public Guid? ProjectId { get; set; }
        public Guid? LocationId { get; set; }
        public string? Discipline { get; set; }
        public Guid? BasePositionId { get; set; }
        public string? Sector { get; set; }
        public string Unit { get; set; } = null!;
        public Guid? ResponsibleId { get; set; }

        public class Handler : IRequestHandler<CreateResponsibilityMatrix, QueryResponsibilityMatrix>
        {
            private readonly ResourcesDbContext resourcesDb;
            private readonly IProfileService profileService;
            private readonly IProjectOrgResolver orgResolver;
            private readonly ILineOrgResolver lineOrgResolver;

            public Handler(ResourcesDbContext resourcesDb, IProfileService profileService, IProjectOrgResolver orgResolver, ILineOrgResolver lineOrgResolver)
            {
                this.resourcesDb = resourcesDb;
                this.profileService = profileService;
                this.orgResolver = orgResolver;
                this.lineOrgResolver = lineOrgResolver;
            }

            public async Task<QueryResponsibilityMatrix> Handle(CreateResponsibilityMatrix request,
                CancellationToken cancellationToken)
            {
                DbProject? project = null;
                if (request.ProjectId != null)
                {
                    project = await EnsureProjectAsync(request.ProjectId.Value);
                    if (project == null)
                        throw new ArgumentException("Unable to resolve project using org service");
                }

                DbPerson? responsible = await GetResourceOwner(request.Unit);

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

                resourcesDb.ResponsibilityMatrices.Add(newItem);
                await resourcesDb.SaveChangesAsync(cancellationToken);

                return new QueryResponsibilityMatrix(newItem);
            }

            private async Task<DbPerson?> GetResourceOwner(string departmentId)
            {
                var department = await lineOrgResolver.ResolveDepartmentAsync(departmentId);
                if (department?.Manager?.AzureUniqueId is not null)
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
