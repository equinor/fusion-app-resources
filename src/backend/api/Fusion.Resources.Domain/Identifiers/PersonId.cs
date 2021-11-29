using Fusion.ApiClients.Org;
using Fusion.Resources.Database.Entities;
using System;

#nullable enable
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

        public static implicit operator PersonId(PersonnelId personId)
        {
            return new PersonId(personId.OriginalIdentifier);
        }

        public static implicit operator Fusion.Integration.Profile.PersonIdentifier(PersonId personId)
        {
            return Fusion.Integration.Profile.PersonIdentifier.Parse(personId.OriginalIdentifier);
        }

        public static implicit operator PersonId?(ApiPersonV2? assignedPerson)
        {
            if (assignedPerson is null)
                return null;
            //throw new ArgumentNullException(nameof(assignedPerson), "Assigned persin is null. Must provide value when implicitly converting");

            return new PersonId(assignedPerson.AzureUniqueId.HasValue switch
            {
                true => $"{assignedPerson.AzureUniqueId}",
                _ => assignedPerson.Mail
            });
        }

        public static implicit operator ApiPersonV2?(PersonId? personId)
        {
            if (personId == null)
                return null;

            return personId.Value.Type switch
            {
                PersonId.IdentifierType.UniqueId => new ApiPersonV2 { AzureUniqueId = personId.Value.UniqueId },
                _ => new ApiPersonV2 { Mail = personId.Value.OriginalIdentifier }
            };
        }

        public static implicit operator PersonId?(DbExternalPersonnelPerson? person)
        {
            if (person is null)
                return null;

            return person.AzureUniqueId.HasValue switch
            {
                true => new PersonId(person.AzureUniqueId.Value),
                _ => new PersonId(person.Mail)
            };
        }
    }
}
