using System;

namespace Fusion.Resources.Domain
{
    public struct PersonId
    {
        public PersonId(string identifier)
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

        public PersonId(Guid uniqueId)
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


        public static implicit operator PersonId(string identifier)
        {
            return new PersonId(identifier);
        }
        public static implicit operator PersonId(Guid uniqueId)
        {
            return new PersonId(uniqueId);
        }
    }
}
