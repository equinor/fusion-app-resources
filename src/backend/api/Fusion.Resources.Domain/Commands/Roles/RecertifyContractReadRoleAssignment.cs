using Fusion.Integration.Roles;
using Fusion.Resources.Database;
using MediatR;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Notifications
{
    public class RecertifyContractReadRoleAssignment : IRequest
    {
        public RecertifyContractReadRoleAssignment(Guid delegatedRoleId)
        {
            DelegatedRoleId = delegatedRoleId;
        }

        public Guid DelegatedRoleId { get; }


        public class Handler : AsyncRequestHandler<CreateContractReadRoleAssignment>
        {
            private readonly ResourcesDbContext resourcesDb;
            private readonly IFusionRolesClient fusionRolesClient;
            private readonly IMediator mediator;
            private readonly TelemetryClient telemetryClient;

            public Handler(ResourcesDbContext resourcesDb, IFusionRolesClient fusionRolesClient, IMediator mediator, TelemetryClient telemetryClient)
            {
                this.resourcesDb = resourcesDb;
                this.fusionRolesClient = fusionRolesClient;
                this.mediator = mediator;
                this.telemetryClient = telemetryClient;
            }

            protected override async Task Handle(CreateContractReadRoleAssignment notification, CancellationToken cancellationToken)
            {
                var roleAssignment = await resourcesDb.DelegatedRoles
                    .Include(r => r.Person)
                    .Include(r => r.Contract)
                    .FirstOrDefaultAsync(r => r.Id == notification.DelegatedRoleId);

                if (roleAssignment == null)
                    throw new ArgumentException($"Could not locate delegated role with id '{notification.DelegatedRoleId}' when trying to create role assignment");

                try
                {
                    await fusionRolesClient.UpdateRoleByIdentifierAsync($"{roleAssignment.Id}", r => r.UpdateValidTo(roleAssignment.ValidTo.UtcDateTime));
                }
                catch (RoleNotFoundError)
                {
                    telemetryClient.TrackTrace("Role was not found when trying to update. It must have expired - creating new...");

                    // If the role was not updated, it might have expired or not been created - ensure new exists.
                    await mediator.Publish(new CreateContractReadRoleAssignment(notification.DelegatedRoleId));
                }                
                catch (Exception ex)
                {
                    telemetryClient.TrackException(ex);
                    throw new IntegrationError("Could not recertify role for person.", ex);
                }
            }
        }
    }
}
