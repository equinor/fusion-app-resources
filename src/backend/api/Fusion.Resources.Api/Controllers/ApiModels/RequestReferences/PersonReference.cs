using Fusion.Resources.Domain;
using System;

namespace Fusion.Resources.Api.Controllers
{
    public class PersonReference
    {
        public Guid? AzureUniquePersonId { get; set; }
        public string Mail { get; set; }


        public static implicit operator PersonId?(PersonReference? personReference)
        {
            if (personReference == null)
                return null;

            if (personReference.AzureUniquePersonId.HasValue)
                return new PersonId(personReference.AzureUniquePersonId.Value);

            return new PersonId(personReference.Mail);
        }
    }
}
