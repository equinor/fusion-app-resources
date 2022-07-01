using Fusion.Integration.Profile;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Resources.Domain
{
    public class PersonsNotFoundError : Exception
    {
        public PersonsNotFoundError() : base("Failed to resolve one or more persons.") { }
        public PersonsNotFoundError(IEnumerable<PersonId> personIds) : base("Failed to resolve one or more persons.")
        {
            MissingIdentifiers = personIds.Select(x => x.OriginalIdentifier).ToList();
        }

        public PersonsNotFoundError(IEnumerable<ResolvedPersonProfile> unresolvedProfiles)
        {
            MissingIdentifiers = unresolvedProfiles.Select(x => x.Identifier.ToString()).ToList();
        }

        public List<string> MissingIdentifiers
        {
            get
            {
                if (Data.Contains("MissingIdentifiers"))
                {
                    return (List<string>)Data["MissingIdentifiers"]!;
                }
                return new List<string>();
            }
            set
            {
                Data["MissingIdentifiers"] = value;
            }
        }

        internal static void ThrowWhenAnyFailed(IEnumerable<ResolvedPersonProfile>? resolved)
        {
            if (resolved is null) throw new PersonsNotFoundError();

            var failed = resolved.Where(x => !x.Success);
            if (failed.Any()) throw new PersonsNotFoundError(failed);
        }
    }
}
