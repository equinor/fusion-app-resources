using System;
using System.Text;

namespace Fusion.Resources.Database.Entities
{


    public class DbPerson
    {
        /// <summary>
        /// This is the local person id. Not to be confused with fusion person id, azure id etc.
        /// </summary>
        public Guid Id { get; set; }

        public Guid AzureUniqueId { get; set; }

        public string Mail { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }

        public string AccountType { get; set; }
        public string JobTitle { get; set; }
    }

}
