using Fusion.Resources.Database.Entities;
using System;

namespace Fusion.Resources.Domain
{
    public class QueryConversationMessage
    {
        public QueryConversationMessage(DbConversationMessage entity)
        {
            Id = entity.Id;
            Title = entity.Title;
            Body = entity.Body;
            Category = entity.Category;

            SenderId = entity.SenderId;
            Sender = new QueryPerson(entity.Sender);

            RequestId = entity.RequestId;

            Recipient = entity.Recpient.MapToDomain();
        }

        public Guid Id { get; }
        public string Title { get; }
        public string Body { get; }
        public string Category { get; }

        public Guid SenderId { get; }
        public QueryPerson Sender { get; }

        public Guid RequestId { get; }
        public QueryMessageRecipient Recipient { get; }
    }

    public enum QueryMessageRecipient { ResourceOwner, TaskOwner, Both }
}
