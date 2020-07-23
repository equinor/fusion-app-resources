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
        public AddComment(Guid requestId, string comment)
        {
            RequestId = requestId;
            Comment = comment;
        }

        public Guid RequestId { get; }

        public string Comment { get; }

        public class Handler : AsyncRequestHandler<AddComment>
        {
            private readonly ResourcesDbContext db;
            private readonly IMediator mediator;

            public Handler(ResourcesDbContext db, IMediator mediator)
            {
                this.db = db;
                this.mediator = mediator;
            }

            protected override async Task Handle(AddComment command, CancellationToken cancellationToken)
            {
                var contractorRequest = await db.ContractorRequests.FirstOrDefaultAsync(c => c.Id == command.RequestId);

                if (contractorRequest == null)
                    throw new InvalidOperationException($"Contractor request with id '{command.RequestId}' was not found");

                var origin = command.Editor.Person.AccountType.ToLower() switch
                {
                    "consultant" => DbRequestComment.DbOrigin.Company,
                    "employee" => DbRequestComment.DbOrigin.Company,
                    "external" => DbRequestComment.DbOrigin.Contractor,
                    _ => throw new InvalidOperationException("Unable to resolve origin. Aborting add operation.")
                };

                var comment = new DbRequestComment
                {
                    Id = Guid.NewGuid(),
                    Comment = command.Comment,
                    Origin = origin,
                    Created = DateTimeOffset.UtcNow,
                    CreatedById = command.Editor.Person.Id,
                    RequestId = command.RequestId
                };

                await db.RequestComments.AddAsync(comment);
                await db.SaveChangesAsync();

                await mediator.Publish(new Notifications.CommentAdded(comment.Id));
            }
        }
    }
}
