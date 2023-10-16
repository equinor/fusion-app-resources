using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

#nullable enable 

namespace Fusion.Resources.Domain.Commands
{
    public class DeletePersonNote : TrackableRequest
    {
        public DeletePersonNote(Guid noteId, Guid personAzureUniqueId)
        {
            NoteId = noteId;
            PersonAzureUniqueId = personAzureUniqueId;
        }

        public Guid NoteId { get; }
        public Guid PersonAzureUniqueId { get; }

        public class Handler : IRequestHandler<DeletePersonNote>
        {
            private readonly ResourcesDbContext dbContext;

            public Handler(ResourcesDbContext dbContext)
            {
                this.dbContext = dbContext;
            }

            public async Task Handle(DeletePersonNote request, CancellationToken cancellationToken)
            {
                var note = await dbContext.PersonNotes.FirstOrDefaultAsync(n => n.Id == request.NoteId && n.AzureUniqueId == request.PersonAzureUniqueId);
                if (note is null)
                    throw new ArgumentException("Could not locate note to delete");

                dbContext.Remove(note);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
