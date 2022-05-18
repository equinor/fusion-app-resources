using Fusion.Resources.Domain;
using System;

namespace Fusion.Resources.Api.Controllers
{
    public class PersonReference
    {
        public Guid? AzureUniquePersonId { get; set; }
        public string? Mail { get; set; }

        public static implicit operator PersonId?(PersonReference? personReference)
        {
            if (personReference == null)
                return null;

            if (personReference.AzureUniquePersonId.HasValue)
                return new PersonId(personReference.AzureUniquePersonId.Value);

            if (string.IsNullOrEmpty(personReference.Mail))
                throw new ArgumentException("Either azure uniwue id or mail has to contain value");

            return new PersonId(personReference.Mail);
        }

        public static implicit operator PersonId(PersonReference personReference)
        {
            if (personReference == null)
                throw new ArgumentNullException("person", "Person cannot be null");


            if (personReference.AzureUniquePersonId.HasValue)
                return new PersonId(personReference.AzureUniquePersonId.Value);

            if (string.IsNullOrEmpty(personReference.Mail))
                throw new ArgumentException("Either azure uniwue id or mail has to contain value");

            return new PersonId(personReference.Mail);
        }
    }
}
