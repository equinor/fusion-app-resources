using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fusion.Resources.Domain.Notifications
{
    public class ContractRoleDelegated : INotification
    {
        public ContractRoleDelegated(Guid roleId)
        {
            RoleId = roleId;
        }

        public Guid RoleId { get; }
    }
}
