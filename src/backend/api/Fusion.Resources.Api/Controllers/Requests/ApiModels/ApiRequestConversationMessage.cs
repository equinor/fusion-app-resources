using Fusion.Resources.Domain;
using System;
using System.Text.Json.Serialization;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiRequestConversationMessage
    {
        public ApiRequestConversationMessage(QueryConversationMessage message)
        {
            Id = message.Id;
            Title = message.Title;
            Body = message.Body;
            Category = message.Category;

            Recipient = message.Recipient switch {
                QueryMessageRecipient.ResourceOwner => ApiMessageRecipient.ResourceOwner,
                QueryMessageRecipient.TaskOwner => ApiMessageRecipient.TaskOwner,
                _ => throw new NotSupportedException($"Recipient type '{message.Recipient}' is not supported")
            };

            SenderId = message.SenderId;
            Sender = new ApiPerson(message.Sender);
            Sent = message.Sent;

            RequestId = message.RequestId;
            Properties = message.Properties is not null
                ? new ApiPropertiesCollection(message.Properties)
                : null;
        }

        public Guid Id { get; }
        public string Title { get; }
        public string Body { get; }
        public string Category { get; }

        public ApiMessageRecipient Recipient { get; }

        public Guid SenderId { get; }
        public ApiPerson Sender { get; }
        public DateTimeOffset Sent { get; }
        public Guid RequestId { get; }

        public ApiPropertiesCollection? Properties { get;  }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ApiMessageRecipient { ResourceOwner, TaskOwner }
}
