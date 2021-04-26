using Fusion.Resources.Domain;
using System;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiPersonNote
    {
        public ApiPersonNote(QueryPersonNote note)
        {
            Id = note.Id;
            Content = note.Content;
            Title = note.Title;
            IsShared = note.IsShared;
            Updated = note.Updated;
            UpdatedBy = new ApiPerson(note.UpdatedBy);
        }

        public Guid Id { get; set; }
        public string? Content { get; set; }
        public string? Title { get; set; }
        public bool IsShared { get; set; }
        public DateTimeOffset Updated { get; set; }
        public ApiPerson UpdatedBy { get; set; } = null!;
    }


}