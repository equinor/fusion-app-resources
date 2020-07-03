using FluentAssertions;
using System;
using System.Linq;
using System.Security.Claims;

namespace Fusion.Resources.Api.Tests
{
    public static class ClaimCreatorExtensions
    {
        public static ClaimBuilder AddClaim(this ClaimsIdentity identity, string name, string value)
        {
            var builder = new ClaimBuilder(identity);
            return builder.AddClaim(name, value);
        }
        public static ClaimBuilder AddClaim(this ClaimsIdentity identity, string name, Guid value) => identity.AddClaim(name, $"{value}");

        public class ClaimBuilder
        {
            private readonly ClaimsIdentity identity;
            private Claim? claim;
            private readonly Guid claimId = Guid.NewGuid();

            public ClaimBuilder(ClaimsIdentity identity)
            {
                this.identity = identity;
            }

            public ClaimBuilder AddClaim(string name, string value)
            {
                claim = new Claim(name, value);
                claim.Properties.Add("claimId", $"{claimId}");

                identity.AddClaim(claim);
                return this;
            }

            public ClaimBuilder WithProperty(string property, Guid value) => WithProperty(property, $"{value}");
            public ClaimBuilder WithProperty(string property, bool value) => WithProperty(property, $"{value}");


            public ClaimBuilder WithProperty(string property, string? value)
            {
                // Seems like the claim is copied onto the identity, so cannot update the reference kept in this type.
                if (claim != null)
                {
                    identity.Claims.FirstOrDefault(c => c.Properties.ContainsKey("claimId") && c.Properties["claimId"] == $"{claimId}")?.Properties.Add(property, value);
                }

                return this;
            }
        }
    }
}
