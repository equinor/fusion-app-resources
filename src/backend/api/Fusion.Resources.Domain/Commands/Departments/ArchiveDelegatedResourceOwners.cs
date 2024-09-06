using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Integration.Profile;
using Fusion.Integration.Roles;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain.Notifications.System;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Fusion.Resources.Domain.Commands.Departments;

/// Archive delegated resource owners for the department and remove their roles.
public class ArchiveDelegatedResourceOwners : TrackableRequest
{
    public LineOrgId DepartmentId { get; private init; }
    public ICollection<Guid>? ResourceOwnersToArchive { get; private set; }


    public ArchiveDelegatedResourceOwners(LineOrgId departmentId)
    {
        DepartmentId = departmentId;
    }

    /// Only archive the resource owners with the provided Azure Object Ids
    public ArchiveDelegatedResourceOwners WhereResourceOwnersAzureId(ICollection<Guid> resourceOwners)
    {
        ArgumentNullException.ThrowIfNull(resourceOwners);
        ResourceOwnersToArchive = resourceOwners;
        return this;
    }


    public class ArchiveDelegatedResourceOwnersHandler : IRequestHandler<ArchiveDelegatedResourceOwners>
    {
        private readonly ILogger<ArchiveDelegatedResourceOwnersHandler> logger;
        private readonly ResourcesDbContext db;
        private readonly IFusionRolesClient rolesClient;


        public ArchiveDelegatedResourceOwnersHandler(ILogger<ArchiveDelegatedResourceOwnersHandler> logger, ResourcesDbContext db, IFusionRolesClient rolesClient)
        {
            this.logger = logger;
            this.db = db;
            this.rolesClient = rolesClient;
        }

        public async Task Handle(ArchiveDelegatedResourceOwners request, CancellationToken cancellationToken)
        {
            var delegatedResourceOwnersToArchive = await db.DelegatedDepartmentResponsibles
                .Where(r => r.DepartmentId == request.DepartmentId.FullDepartment)
                .Where(r => request.ResourceOwnersToArchive == null || request.ResourceOwnersToArchive.Contains(r.ResponsibleAzureObjectId))
                .ToListAsync(cancellationToken);

            if (delegatedResourceOwnersToArchive.Count == 0)
                return;

            foreach (var resourceOwner in delegatedResourceOwnersToArchive)
            {
                try
                {
                    await rolesClient.DeleteRolesAsync(
                        new PersonIdentifier(resourceOwner.ResponsibleAzureObjectId),
                        q => q.WhereRoleName(AccessRoles.ResourceOwner).WhereScopeValue(request.DepartmentId.FullDepartment)
                    );
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Failed to delete role for delegated resource owner {AzureUniqueId} in department {FullDepartment}",
                        resourceOwner.ResponsibleAzureObjectId, request.DepartmentId.FullDepartment);
                    // TODO: Should we stop the execution here? Use transactions?
                    // throw;
                }
            }


            db.DelegatedDepartmentResponsibles.RemoveRange(delegatedResourceOwnersToArchive);

            var archivedDelegateResourceOwners = delegatedResourceOwnersToArchive
                .Select(res => new DbDelegatedDepartmentResponsibleHistory(res));

            db.DelegatedDepartmentResponsiblesHistory.AddRange(archivedDelegateResourceOwners);

            await db.SaveChangesAsync(CancellationToken.None);
        }
    }
}