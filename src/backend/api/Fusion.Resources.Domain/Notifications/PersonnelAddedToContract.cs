using MediatR;
using System;

namespace Fusion.Resources.Domain.Notifications
{
    public class PersonnelAddedToContract : INotification
    {
        public PersonnelAddedToContract(Guid personnelId)
        {
            ContractPersonnelId = personnelId;
        }

        public Guid ContractPersonnelId { get; }
    }
}
