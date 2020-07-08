using Fusion.Integration.Roles;
using Fusion.Resources.Database;
using MediatR;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    public class CreateContractReadRoleAssignment : IRequest
    {
        public CreateContractReadRoleAssignment(Guid delegatedRoleId)
        {
            if (delegatedRoleId == Guid.Empty)
                throw new ArgumentException("Role cannot be empty when creating role assignment");

            DelegatedRoleId = delegatedRoleId;
        }

        public Guid DelegatedRoleId { get; }


        public class Handler : AsyncRequestHandler<CreateContractReadRoleAssignment>
        {
            private readonly ResourcesDbContext resourcesDb;
            private readonly IFusionRolesClient fusionRolesClient;
            private readonly TelemetryClient telemetryClient;

            public Handler(ResourcesDbContext resourcesDb, IFusionRolesClient fusionRolesClient, TelemetryClient telemetryClient)
            {
                this.resourcesDb = resourcesDb;
                this.fusionRolesClient = fusionRolesClient;
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

                // Create role assignment
                try
                {
                    var newassignment = await fusionRolesClient.AssignRoleAsync(roleAssignment.Person.AzureUniqueId, r =>
                    {
                        r.RoleName = "Fusion.Contract.Read";
                        r.Identifier = $"{roleAssignment.Id}";
                        r.Scope = ("Contract", $"{roleAssignment.Contract.OrgContractId}");
                        r.ValidTo = roleAssignment.ValidTo;
                    });
                }
                catch (Exception ex)
                {
                    telemetryClient.TrackException(ex);
                    throw new IntegrationError("Could not assign role to person.", ex);
                }
            }
        }
    }
}
