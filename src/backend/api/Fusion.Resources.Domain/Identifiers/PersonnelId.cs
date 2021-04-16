using Fusion.Integration.Profile;
using System;

namespace Fusion.Resources.Domain
{
    public struct PersonnelId
    {
        public PersonnelId(string identifier)
        {
            OriginalIdentifier = identifier;

            if (Guid.TryParse(identifier, out Guid id))
            {
                UniqueId = id;
                Mail = null;
                Type = IdentifierType.UniqueId;
            }
            else
            {
                UniqueId = null;
                Mail = identifier;
                Type = IdentifierType.Mail;
            }
        }

        public PersonnelId(Guid uniqueId)
        {
            OriginalIdentifier = $"{uniqueId}";
            UniqueId = uniqueId;
            Mail = null;
            Type = IdentifierType.UniqueId;
        }

        public Guid? UniqueId { get; set; }
        public string OriginalIdentifier { get; set; }
        public string? Mail { get; set; }
        public IdentifierType Type { get; set; }

        public enum IdentifierType { UniqueId, Mail }


        public static implicit operator PersonnelId(string identifier)
        {
            return new PersonnelId(identifier);
        }
        public static implicit operator PersonnelId(Guid uniqueId)
        {
            return new PersonnelId(uniqueId);
        }

        public static implicit operator PersonIdentifier(PersonnelId personId)
        {
            return PersonIdentifier.Parse(personId.OriginalIdentifier);
        }
    }

}
