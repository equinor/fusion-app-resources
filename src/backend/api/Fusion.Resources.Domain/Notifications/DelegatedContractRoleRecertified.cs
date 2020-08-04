using MediatR;
using System;

namespace Fusion.Resources.Domain.Notifications
{
    public class DelegatedContractRoleRecertified : INotification
    {
        public DelegatedContractRoleRecertified(Guid roleId)
        {
            RoleId = roleId;
        }

        public Guid RoleId { get; }
    }
}
