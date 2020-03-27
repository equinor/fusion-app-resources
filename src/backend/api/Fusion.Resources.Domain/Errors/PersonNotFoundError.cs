using System;

namespace Fusion.Resources
{
    public class PersonNotFoundError : Exception
    {
        public PersonNotFoundError(string personIdentifier) : base($"Person with identifier '{personIdentifier}' was not found")
        {
            PersonIdentifier = personIdentifier;
        }

        public string PersonIdentifier { get; }
    }
}
