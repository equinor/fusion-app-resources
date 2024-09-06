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
using Fusion.Resources.Domain.Commands.Departments;

namespace Fusion.Resources.Domain.Notifications.System;

public partial class OrgUnitDeleted
{
    /// <summary>
    ///     Archive all delegated resource owners for the deleted department and remove their roles.
    /// </summary>
    public class ArchiveDelegatedResourceOwnersHandler : INotificationHandler<OrgUnitDeleted>
    {
        private readonly ILogger<ArchiveDelegatedResourceOwnersHandler> logger;
        private readonly IMediator mediator;

        public ArchiveDelegatedResourceOwnersHandler(ILogger<ArchiveDelegatedResourceOwnersHandler> logger, IMediator mediator)
        {
            this.logger = logger;
            this.mediator = mediator;
        }

        public async Task Handle(OrgUnitDeleted notification, CancellationToken cancellationToken)
        {
            logger.LogInformation("Archiving delegated resource owners for deleted department {FullDepartment}", notification.FullDepartment);

            using var systemAccountScope = mediator.SystemAccountScope();

            await mediator.Send(new ArchiveDelegatedResourceOwners(new LineOrgId()
            {
                FullDepartment = notification.FullDepartment,
                SapId = notification.SapId
            }), cancellationToken);
        }
    }
}