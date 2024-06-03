using Fusion.Integration.Org;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands.BaseHandlers
{
    public class ResponsibilityMatrixBaseHandler
    {
        protected readonly ResourcesDbContext resourcesDb;
        protected readonly IProfileService profileService;
        protected readonly IProjectOrgResolver orgResolver;
        protected readonly IMediator mediator;

        public ResponsibilityMatrixBaseHandler(ResourcesDbContext resourcesDb, IProfileService profileService, IProjectOrgResolver orgResolver, IMediator mediator)
        {
            this.resourcesDb = resourcesDb;
            this.profileService = profileService;
            this.orgResolver = orgResolver;
            this.mediator = mediator;
        }

        protected async Task<DbPerson?> GetResourceOwner(string departmentId)
        {
            var resourceOwner = await mediator.Send(new ResolveDepartmentResourceOwner(departmentId));

            if (resourceOwner?.MainManager?.AzureUniqueId is not null)
            {
                return await profileService.EnsurePersonAsync(new PersonId(resourceOwner.MainManager.AzureUniqueId));
            }

            return null;
        }

        protected async Task<DbProject?> EnsureProjectAsync(Guid projectId)
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
