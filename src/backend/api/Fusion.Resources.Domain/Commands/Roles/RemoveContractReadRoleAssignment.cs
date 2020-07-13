using Fusion.Integration.Roles;
using MediatR;
using Microsoft.ApplicationInsights;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    public class RemoveContractReadRoleAssignment : IRequest
    {
        public RemoveContractReadRoleAssignment(Guid delegatedRoleId)
        {
            DelegatedRoleId = delegatedRoleId;
        }

        public Guid DelegatedRoleId { get; }


        public class Handler : AsyncRequestHandler<RemoveContractReadRoleAssignment>
        {
            private readonly IFusionRolesClient fusionRolesClient;
            private readonly TelemetryClient telemetryClient;

            public Handler(IFusionRolesClient fusionRolesClient, TelemetryClient telemetryClient)
            {
                this.fusionRolesClient = fusionRolesClient;
                this.telemetryClient = telemetryClient;
            }

            protected override async Task Handle(RemoveContractReadRoleAssignment notification, CancellationToken cancellationToken)
            {
                try 
                {                    
                    var deletedRoles = await fusionRolesClient.DeleteRoleByIdentifierAsync($"{notification.DelegatedRoleId}");
                    telemetryClient.TrackTrace($"Deleted roles: {JsonConvert.SerializeObject(deletedRoles, Formatting.Indented)}");
                }
                catch (Exception ex)
                {
                    telemetryClient.TrackException(ex);
                    throw new IntegrationError("Could not remove contract role assignment for person.", ex);
                }
            }
        }
    }
}
