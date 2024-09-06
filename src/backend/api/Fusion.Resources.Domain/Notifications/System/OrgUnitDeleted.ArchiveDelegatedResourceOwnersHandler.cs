using System;
using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Integration.Profile;
using Fusion.Integration.Roles;
using Fusion.Resources.Database.Entities;

namespace Fusion.Resources.Domain.Notifications.System;

public partial class OrgUnitDeleted
{
    /// <summary>
    ///     Archive all delegated resource owners for the deleted department and remove their roles.
    /// </summary>
    public class ArchiveDelegatedResourceOwnersHandler : INotificationHandler<OrgUnitDeleted>
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

        public async Task Handle(OrgUnitDeleted notification, CancellationToken cancellationToken)
        {
            var delegatedResourceOwners = await db.DelegatedDepartmentResponsibles
                .Where(r => r.DepartmentId == notification.FullDepartment)
                .ToListAsync(cancellationToken);

            if (delegatedResourceOwners.Count == 0)
                return;

            logger.LogInformation("Archiving {Count} delegated resource owners for deleted department {FullDepartment}", delegatedResourceOwners.Count, notification.FullDepartment);


            foreach (var resourceOwner in delegatedResourceOwners)
            {
                try
                {
                    await rolesClient.DeleteRolesAsync(
                        new PersonIdentifier(resourceOwner.ResponsibleAzureObjectId),
                        q => q.WhereRoleName(AccessRoles.ResourceOwner).WhereScopeValue(notification.FullDepartment)
                    );
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Failed to delete role for delegated resource owner {AzureUniqueId} in department {FullDepartment}", resourceOwner.ResponsibleAzureObjectId, notification.FullDepartment);
                    // TODO: Should we stop the execution here? Use transactions?
                    // throw;
                }
            }


            db.DelegatedDepartmentResponsibles.RemoveRange(delegatedResourceOwners);

            var archivedDelegateResourceOwners = delegatedResourceOwners
                .Select(res => new DbDelegatedDepartmentResponsibleHistory(res));

            db.DelegatedDepartmentResponsiblesHistory.AddRange(archivedDelegateResourceOwners);

            await db.SaveChangesAsync(CancellationToken.None);
        }
    }
}