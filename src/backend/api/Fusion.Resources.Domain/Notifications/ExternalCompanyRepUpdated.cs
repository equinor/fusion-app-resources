using MediatR;
using System;

namespace Fusion.Resources.Domain.Notifications
{
    public class ExternalCompanyRepUpdated : INotification
    {
        public ExternalCompanyRepUpdated(Guid positionId)
        {
            PositionId = positionId;
        }

        public Guid PositionId { get; }
    }
}
