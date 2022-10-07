using Fusion.Integration.Profile;
using System;

namespace Fusion.Resources.Domain
{
    public class QueryProposedPerson
    {
        public DateTimeOffset ProposedDate { get; set; }
        public Guid AzureUniqueId { get; set; }
        public string? Mail { get; set; }

        public FusionPersonProfile? Person { get; set; }

        public FusionPersonProfile? ResourceOwner { get; set; }

        public FusionPersonProfile? DelegatedResourceOwner { get; set; }

        public bool WasNotified { get; set; }
    }
}