using MediatR;
using System;

namespace Fusion.Resources.Domain.Notifications
{
    public class PersonnelAddedToContract : INotification
    {
        public PersonnelAddedToContract(Guid orgContractId, Guid personnelId)
        {
            OrgContractId = orgContractId;
            ContractPersonnelId = personnelId;
        }

        public Guid OrgContractId { get; }

        public Guid ContractPersonnelId { get; }
    }
}
