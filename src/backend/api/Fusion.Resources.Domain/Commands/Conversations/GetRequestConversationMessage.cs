using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class GetRequestConversationMessage : IRequest<QueryConversationMessage>
    {
        private Guid requestId;
        private Guid messageId;


        public GetRequestConversationMessage(Guid requestId, Guid messageId)
        {
            this.requestId = requestId;
            this.messageId = messageId;
        }

        public class Handler : IRequestHandler<GetRequestConversationMessage, QueryConversationMessage?>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            public async Task<QueryConversationMessage?> Handle(GetRequestConversationMessage request, CancellationToken cancellationToken)
            {
                var message = await db.RequestConversations
                    .Include(m => m.Sender)
                    .Where(m => m.RequestId == request.requestId)
                    .SingleOrDefaultAsync(m => m.Id == request.messageId, cancellationToken);

                return message is not null
                    ? new QueryConversationMessage(message)
                    : null;
            }

        }
    }
}
