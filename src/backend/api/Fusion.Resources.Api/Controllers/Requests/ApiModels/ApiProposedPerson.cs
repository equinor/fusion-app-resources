using Fusion.Resources.Domain;
using System;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiProposedPerson
    {
        public ApiProposedPerson(QueryResourceAllocationRequest.QueryProposedPerson proposedPerson)
        {
            ProposedAt = proposedPerson.ProposedDate;
            WasNotified = proposedPerson.WasNotified;
            Person = new ApiPerson(proposedPerson.AzureUniqueId, proposedPerson.Mail!);

            if (proposedPerson.ResourceOwner is not null)
            {
                ResourceOwner = new ApiPerson(proposedPerson.ResourceOwner);
            }
        }


        public DateTimeOffset ProposedAt { get; set; }
        public ApiPerson Person { get; set; }
        public bool WasNotified { get; set; }

        public ApiPerson? ResourceOwner { get; set; }
    }
}