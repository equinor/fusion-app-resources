using Fusion.Resources.Domain;
using System;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiProposedPerson
    {
        public ApiProposedPerson(QueryProposedPerson proposedPerson)
        {
            ProposedAt = proposedPerson.ProposedDate;
            WasNotified = proposedPerson.WasNotified;
            Person = new ApiPerson(proposedPerson.AzureUniqueId, proposedPerson.Mail!);

            if (proposedPerson.Person is not null)
                Person = new ApiPerson(proposedPerson.Person);

            if (proposedPerson.ResourceOwner is not null)
            {
                ResourceOwner = new ApiPerson(proposedPerson.ResourceOwner);
                
            }
            if (proposedPerson.DelegatedResourceOwner is not null)
            {
                DelegatedResourceOwner = new ApiPerson(proposedPerson.DelegatedResourceOwner);
            }
        }

        public DateTimeOffset ProposedAt { get; set; }
        public ApiPerson Person { get; set; }
        public bool WasNotified { get; set; }
        public ApiPerson? ResourceOwner { get; set; }
        public ApiPerson? DelegatedResourceOwner { get; set; }
    }
}