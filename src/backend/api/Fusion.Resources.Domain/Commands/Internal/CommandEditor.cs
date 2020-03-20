using Fusion.Resources.Database.Entities;
using System;

namespace Fusion.Resources.Domain.Commands
{
    public class CommandEditor
    {
        public CommandEditor(Guid? azureUniqueId, DbPerson person)
        {
            AzureUniqueId = azureUniqueId;
            Person = person;
        }

        public Guid? AzureUniqueId { get; set; }
        public DbPerson Person { get; set; }
    }
}
