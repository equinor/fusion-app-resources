using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    public class AddComment : TrackableRequest<QueryRequestComment>
    {
        public AddComment(RequestType type, Guid requestId, string comment)
        {
            Type = type;
            RequestId = requestId;
            Comment = comment;
        }

        public RequestType Type { get; }
        public Guid RequestId { get; }

        public string Comment { get; }

        public class Handler : IRequestHandler<AddComment, QueryRequestComment>
        {
            private readonly ResourcesDbContext db;
            private readonly IMediator mediator;

            public Handler(ResourcesDbContext db, IMediator mediator)
            {
                this.db = db;
                this.mediator = mediator;
            }

            public async Task<QueryRequestComment> Handle(AddComment command, CancellationToken cancellationToken)
            {
                switch (command.Type)
                {
                    case RequestType.Internal:
                        var request = await db.ResourceAllocationRequests.FirstOrDefaultAsync(c => c.Id == command.RequestId);

                        if (request == null)
                            throw new InvalidOperationException($"Internal equest with id '{command.RequestId}' was not found");
                        break;

                    default:
                        var contractorRequest = await db.ContractorRequests.FirstOrDefaultAsync(c => c.Id == command.RequestId);

                        if (contractorRequest == null)
                            throw new InvalidOperationException($"Contractor request with id '{command.RequestId}' was not found");
                        break;

                }


                var origin = command.Editor.Person.AccountType.ToLower() switch
                {
                    "consultant" => DbRequestComment.DbOrigin.Company,
                    "employee" => DbRequestComment.DbOrigin.Company,
                    "external" => DbRequestComment.DbOrigin.Contractor,
                    _ => DbRequestComment.DbOrigin.Internal
                    //_ => throw new InvalidOperationException("Unable to resolve origin. Aborting add operation.")
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

                return new QueryRequestComment(comment);
            }
        }
    }

    public enum RequestType
    {
        External, Internal
    }
}
