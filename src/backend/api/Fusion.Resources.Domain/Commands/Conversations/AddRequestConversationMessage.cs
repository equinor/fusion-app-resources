using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain.Commands;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class AddRequestConversationMessage : TrackableRequest<QueryConversationMessage>
    {
        private readonly Guid requestId;
        private readonly string title;
        private readonly string body;
        private readonly string category;
        private readonly QueryMessageRecipient recipient;

        public AddRequestConversationMessage(Guid requestId, string title, string body, string category, QueryMessageRecipient recipient)
        {
            this.requestId = requestId;
            this.title = title;
            this.body = body;
            this.category = category;
            this.recipient = recipient;
        }

        public Dictionary<string, object>? Properties { get; set; }

        public class Handler : IRequestHandler<AddRequestConversationMessage, QueryConversationMessage>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            public async Task<QueryConversationMessage> Handle(AddRequestConversationMessage request, CancellationToken cancellationToken)
            {
                var message = new DbConversationMessage
                {
                    Title = request.title,
                    Body = request.body,
                    Category = request.category,
                    RequestId = request.requestId,
                    SenderId = request.Editor.Person.Id,
                    Recpient = request.recipient.MapToDatabase(),
                    PropertiesJson = request.Properties?.SerializeToStringOrDefault()
                };
                db.RequestConversations.Add(message);
                await db.SaveChangesAsync(cancellationToken);
                
                return new QueryConversationMessage(message);
            }
        }
    }
}
