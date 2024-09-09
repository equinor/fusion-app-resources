using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Integration.Profile;
using Fusion.Integration.Roles;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
        private readonly ResourcesDbContext db;
        private readonly IFusionRolesClient rolesClient;


        public ArchiveDelegatedResourceOwnersHandler(ResourcesDbContext db, IFusionRolesClient rolesClient)
        {
            this.db = db;
            this.rolesClient = rolesClient;
        }

        public async Task Handle(ArchiveDelegatedResourceOwners request, CancellationToken cancellationToken)
        {
            var delegatedResourceOwnersToArchive = await db.DelegatedDepartmentResponsibles
                .Where(r => r.DepartmentId == request.DepartmentId.FullDepartment)
                .Where(r => request.ResourceOwnersToArchive == null || request.ResourceOwnersToArchive.Contains(r.ResponsibleAzureObjectId))
                .ToListAsync(cancellationToken);

            foreach (var resourceOwner in delegatedResourceOwnersToArchive)
            {
                await rolesClient.DeleteRolesAsync(
                    new PersonIdentifier(resourceOwner.ResponsibleAzureObjectId),
                    q => q.WhereRoleName(AccessRoles.ResourceOwner).WhereScopeValue(request.DepartmentId.FullDepartment)
                );

                db.DelegatedDepartmentResponsibles.Remove(resourceOwner);

                var archivedDelegateResourceOwner = new DbDelegatedDepartmentResponsibleHistory(resourceOwner);

                db.DelegatedDepartmentResponsiblesHistory.Add(archivedDelegateResourceOwner);

                await db.SaveChangesAsync(CancellationToken.None);
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
    }
}