using Fusion.Resources.Database;
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

        public GetRequestConversation(Guid requestId)
        {
            this.requestId = requestId;
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
                    .Where(m => m.RequestId == request.requestId)
                    .ToListAsync(cancellationToken);

                return conversation.Select(m => new QueryConversationMessage(m)).ToList();
            }
        }
    }
}
