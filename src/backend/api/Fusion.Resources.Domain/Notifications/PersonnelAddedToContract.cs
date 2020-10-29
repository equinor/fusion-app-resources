using Fusion.ApiClients.Org;
using MediatR;
using System;

namespace Fusion.Resources.Domain.Notifications
{
    public class PersonnelAddedToContract : INotification
    {
        public PersonnelAddedToContract(OrgProjectId orgProjectId, Guid orgContractId, Guid personnelId)
        {
            OrgProjectId = orgProjectId;
            OrgContractId = orgContractId;
            ContractPersonnelId = personnelId;
        }

        public OrgProjectId OrgProjectId { get; }

        public Guid OrgContractId { get; }

        public Guid ContractPersonnelId { get; }
    }
}
