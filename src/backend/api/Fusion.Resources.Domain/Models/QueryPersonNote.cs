using Fusion.Resources.Database.Entities;
using System;

namespace Fusion.Resources.Domain
{
    public class QueryPersonNote
    {
        public QueryPersonNote(DbPersonNote note)
        {
            if (note.UpdatedBy is null)
                throw new ArgumentNullException("Updated by person must be included");

            Id = note.Id;
            PersonAzureUniqueId = note.AzureUniqueId;
            Content = note.Content;
            Title = note.Title;
            Updated = note.Updated;
            UpdatedBy = new QueryPerson(note.UpdatedBy);
            IsShared = note.IsShared;
        }

        public Guid Id { get; set; }
        public Guid PersonAzureUniqueId { get; set; }
        public string? Content { get; set; }
        public string? Title { get; set; }
        public bool IsShared { get; set; }
        public DateTimeOffset Updated { get; set; }
        public QueryPerson UpdatedBy { get; set; } 
    }
}
