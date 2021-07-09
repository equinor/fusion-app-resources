using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands.Conversations
{
    public class UpdateRequestConversationMessage : IRequest<QueryConversationMessage?>
    {
        private Guid requestId;
        private Guid messageId;

        public UpdateRequestConversationMessage(Guid requestId, Guid messageId, string title, string body, string category, QueryMessageRecipient recipientType)
        {
            this.requestId = requestId;
            this.messageId = messageId;
            Title = title;
            Body = body;
            Category = category;
            Recipient = recipientType;
        }

        public string Title { get; set; } = null!;
        public string Body { get; set; } = null!;
        public string Category { get; set; } = null!;
        public QueryMessageRecipient Recipient { get; set; }
        public Dictionary<string, object>? Properties { get; set; }


        public class Handler : IRequestHandler<UpdateRequestConversationMessage, QueryConversationMessage?>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            public async Task<QueryConversationMessage?> Handle(UpdateRequestConversationMessage request, CancellationToken cancellationToken)
            {
                var message = await db.RequestConversations
                    .Include(m => m.Sender)
                    .SingleOrDefaultAsync(m => m.RequestId == request.requestId && m.Id == request.messageId, cancellationToken);

                if (message is null) return null;

                message.Title = request.Title;
                message.Body = request.Body;
                message.Category = request.Category;
                message.Recpient = request.Recipient.MapToDatabase();
                message.PropertiesJson = request.Properties?.SerializeToStringOrDefault();

                await db.SaveChangesAsync(cancellationToken);

                return new QueryConversationMessage(message);
            }
        }
    }
}