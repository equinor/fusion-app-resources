using Fusion.Integration.Roles;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Notifications
{
    public class RemoveContractReadRoleAssignment : IRequest
    {
        public RemoveContractReadRoleAssignment(Guid delegatedRoleId)
        {
            DelegatedRoleId = delegatedRoleId;
        }

        public Guid DelegatedRoleId { get; }


        public class Handler : AsyncRequestHandler<CreateContractReadRoleAssignment>
        {
            private readonly IFusionRolesClient fusionRolesClient;

            public Handler(IFusionRolesClient fusionRolesClient)
            {
                this.fusionRolesClient = fusionRolesClient;
            }

            protected override async Task Handle(CreateContractReadRoleAssignment notification, CancellationToken cancellationToken)
            {
                await fusionRolesClient.DeleteRoleByIdentifierAsync($"{notification.DelegatedRoleId}");
            }
        }
    }
}
