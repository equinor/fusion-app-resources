using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
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

        public class Handler : AsyncRequestHandler<DeletePersonNote>
        {
            private readonly ResourcesDbContext dbContext;

            public Handler(ResourcesDbContext dbContext)
            {
                this.dbContext = dbContext;
            }

            protected override async Task Handle(DeletePersonNote request, CancellationToken cancellationToken)
            {
                dbContext.RemoveRange(
                    dbContext.PersonNotes.Where(n => n.Id == request.NoteId && n.AzureUniqueId == request.PersonAzureUniqueId)
                );

                if (await dbContext.SaveChangesAsync(cancellationToken) <= 0)
                    throw new ArgumentException("Could not locate note to delete");
            }
        }
    }
}
