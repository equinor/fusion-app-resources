using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands.Conversations
{
    public class GetRequestConversation : IRequest<List<QueryConversationMessage>>
    {
        private Guid requestId;
        private readonly DbMessageRecipient recipient;

        public GetRequestConversation(Guid requestId, QueryMessageRecipient recipient)
        {
            this.requestId = requestId;
            this.recipient = recipient.MapToDatabase();
        }

        public class Handler : IRequestHandler<GetRequestConversation, List<QueryConversationMessage>>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            public async Task<List<QueryConversationMessage>> Handle(GetRequestConversation request, CancellationToken cancellationToken)
            {
                var conversation = await db.RequestConversations
                    .Include(m => m.Sender)
                    .Where(m => m.RequestId == request.requestId && m.Recpient == request.recipient)
                    .ToListAsync(cancellationToken);

                return conversation.Select(m => new QueryConversationMessage(m)).ToList();
            }
        }
    }
}
