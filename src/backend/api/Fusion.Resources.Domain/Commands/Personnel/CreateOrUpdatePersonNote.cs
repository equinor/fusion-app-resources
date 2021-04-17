using FluentValidation;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

#nullable enable 

namespace Fusion.Resources.Domain.Commands
{
    public class CreateOrUpdatePersonNote : TrackableRequest<QueryPersonNote> 
    {
        private CreateOrUpdatePersonNote(Guid? id, string? content, bool isShared)
        {
            NoteId = id;
            Content = content;
            IsShared = isShared;
        }

        public Guid? NoteId { get; }
        public string? Content { get; }
        public bool IsShared { get; }

        public string? Title { get; set; }
        public Guid AzureUniqueId { get; set; }

        public CreateOrUpdatePersonNote OnUser(Guid azureUniqueId)
        {
            AzureUniqueId = azureUniqueId;
            return this;
        }
        public CreateOrUpdatePersonNote WithTitle(string? title)
        {
            Title = title;
            return this;
        }

        public static CreateOrUpdatePersonNote CreateNew(string? content, bool isShared) => new CreateOrUpdatePersonNote(null, content, isShared);
        public static CreateOrUpdatePersonNote Update(Guid id, string? content, bool isShared) => new CreateOrUpdatePersonNote(id, content, isShared);

        #region Validation
        public class Validator : AbstractValidator<CreateOrUpdatePersonNote>
        {
            public Validator(ResourcesDbContext dbContext)
            {
                RuleFor(x => x.AzureUniqueId).NotEmpty();

                // Check if note already exist when creating new.
                RuleFor(x => x)
                    .MustAsync(async (x, cancel) =>
                    {
                        var exists = await dbContext.PersonNotes.AnyAsync(n => n.Title == x.Title && n.AzureUniqueId == x.AzureUniqueId && n.Content == x.Content);
                        return !exists;
                    })
                    .When(x => x.NoteId == null)
                    .WithMessage("Note already exist");
            }
        }

        #endregion

        #region Handler
        public class Handler : IRequestHandler<CreateOrUpdatePersonNote, QueryPersonNote>
        {
            private readonly ResourcesDbContext dbContext;

            public Handler(ResourcesDbContext dbContext)
            {
                this.dbContext = dbContext;
            }

            public async Task<QueryPersonNote> Handle(CreateOrUpdatePersonNote request, CancellationToken cancellationToken)
            {
                
                DbPersonNote note;
                if (request.NoteId != null)
                {
                    note = await dbContext.PersonNotes.FirstOrDefaultAsync(p => p.Id == request.NoteId && p.AzureUniqueId == request.AzureUniqueId);
                    if (note is null)
                        throw new ArgumentException("Note id does not exist");
                }
                else
                {
                    note = dbContext.PersonNotes.Add(new DbPersonNote() { AzureUniqueId = request.AzureUniqueId }).Entity;
                }

                note.Content = request.Content;
                note.IsShared = request.IsShared;
                note.Title = request.Title;
                note.Updated = DateTimeOffset.UtcNow;
                note.UpdatedBy = request.Editor.Person;

                await dbContext.SaveChangesAsync(cancellationToken);

                return new QueryPersonNote(note);
            }
        }

        #endregion
    }
}
