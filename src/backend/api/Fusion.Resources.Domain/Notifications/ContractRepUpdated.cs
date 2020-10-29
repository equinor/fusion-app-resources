using MediatR;
using System;

namespace Fusion.Resources.Domain.Notifications
{
    public class ContractRepUpdated : INotification
    {
        public ContractRepUpdated(Guid positionId)
        {
            PositionId = positionId;
        }

        public Guid PositionId { get; }
    }
}
