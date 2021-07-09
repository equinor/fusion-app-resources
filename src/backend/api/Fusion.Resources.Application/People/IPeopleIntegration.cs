using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Resources
{
    public interface IPeopleIntegration
    {
        /// <summary>
        /// Update the preferred contact mail for a user. 
        /// The change is patched to the people api.
        /// </summary>
        /// <param name="azureUniqueId">Azure id for the user to update</param>
        /// <param name="preferredContactMail">The preferred contact mail, can be null</param>
        /// <exception cref="PeopleIntegrationException"></exception>
        Task UpdatePreferredContactMailAsync(Guid azureUniqueId, string? preferredContactMail);
    }

}
