using MediatR;
using System;

namespace Fusion.Resources.Domain.Notifications
{
    public class ExternalContractRepUpdated : INotification
    {
        public ExternalContractRepUpdated(Guid positionId)
        {
            PositionId = positionId;
        }

        public Guid PositionId { get; }
    }
}
