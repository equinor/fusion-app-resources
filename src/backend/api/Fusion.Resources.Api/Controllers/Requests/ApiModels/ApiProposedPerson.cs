using Fusion.Integration.Profile;
using Fusion.Resources.Domain;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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

            if (proposedPerson.DelegatedResourceOwners is not null)
            {
                DelegatedResourceOwners = proposedPerson.DelegatedResourceOwners.Select(d => new ApiPerson(d));
            }
        }

        public DateTimeOffset ProposedAt { get; set; }
        public ApiPerson Person { get; set; }
        public bool WasNotified { get; set; }
        public ApiPerson? ResourceOwner { get; set; }
        public IEnumerable? DelegatedResourceOwners { get; set; }
    }
}