using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    public class AddComment : TrackableRequest
    {
        public AddComment(Guid requestId, string comment, string origin)
        {
            RequestId = requestId;
            Comment = comment;
            Origin = origin;
        }

        public Guid RequestId { get; }

        public string Comment { get; }

        public string Origin { get; }

        public Guid CreatedById { get; set; }

        public class Handler : AsyncRequestHandler<AddComment>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            protected override async Task Handle(AddComment command, CancellationToken cancellationToken)
            {
                var contractorRequest = await db.ContractorRequests.FirstOrDefaultAsync(c => c.Id == command.RequestId);

                if (contractorRequest == null)
                    throw new InvalidOperationException($"Contractor request with id '{command.RequestId}' was not found");

                var comment = new DbRequestComment
                {
                    Id = Guid.NewGuid(),
                    Comment = command.Comment,
                    Origin = command.Origin,
                    Created = DateTimeOffset.UtcNow,
                    CreatedById = command.Editor.Person.Id,
                    RequestId = command.RequestId
                };

                await db.RequestComments.AddAsync(comment);
                await db.SaveChangesAsync();
            }
        }
    }
}
