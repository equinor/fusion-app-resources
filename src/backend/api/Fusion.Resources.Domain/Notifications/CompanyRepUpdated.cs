using MediatR;
using System;

namespace Fusion.Resources.Domain.Notifications
{
    public class CompanyRepUpdated : INotification
    {
        public CompanyRepUpdated(Guid positionId)
        {
            PositionId = positionId;
        }

        public Guid PositionId { get; }
    }
}
