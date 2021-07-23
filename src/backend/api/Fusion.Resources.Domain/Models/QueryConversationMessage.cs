using Fusion.Resources.Database.Entities;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Fusion.Resources.Domain
{
    public class QueryConversationMessage
    {
        private string? propertiesJson;

        public QueryConversationMessage(DbConversationMessage entity)
        {
            Id = entity.Id;
            Title = entity.Title;
            Body = entity.Body;
            Category = entity.Category;

            SenderId = entity.SenderId;
            Sender = new QueryPerson(entity.Sender);
            Sent = entity.Sent;

            RequestId = entity.RequestId;

            Recipient = entity.Recpient.MapToDomain();
            propertiesJson = entity.PropertiesJson;
        }

        public Guid Id { get; }
        public string Title { get; }
        public string Body { get; }
        public string Category { get; }

        public Guid SenderId { get; }
        public QueryPerson Sender { get; }
        public DateTimeOffset Sent { get; }
        public Guid RequestId { get; }
        public QueryMessageRecipient Recipient { get; }


        public Dictionary<string, object> Properties
        {
            get
            {
                if (string.IsNullOrEmpty(propertiesJson)) return new();
                else return JsonSerializer.Deserialize<Dictionary<string, object>>(propertiesJson) ?? new();
            }
        }
    }

    public enum QueryMessageRecipient { ResourceOwner, TaskOwner }
}
