using Fusion.Resources.Database.Entities;
using System;

namespace Fusion.Resources.Domain.Commands
{
    public class CommandEditor
    {
        public Guid? AzureUniqueId { get; set; }
        public DbPerson Person { get; set; }
    }
}
