using Fusion.Resources.Domain;
using System;
using System.Text.Json.Serialization;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiRequestAction
    {
        public ApiRequestAction(QueryRequestAction task)
        {
            Id = task.Id;
            Title = task.Title;
            Body = task.Body;
            Type = task.Type;
            SubType = task.SubType;
            Source = task.Source switch
            {
                QueryTaskSource.ResourceOwner => ApiTaskSource.ResourceOwner,
                QueryTaskSource.TaskOwner => ApiTaskSource.TaskOwner,
                _ => throw new NotSupportedException($"Cannot map {task.Source} to {nameof(ApiTaskSource)}")
            };
            Responsible = task.Responsible switch
            {
                QueryTaskResponsible.ResourceOwner => ApiTaskResponsible.ResourceOwner,
                QueryTaskResponsible.TaskOwner => ApiTaskResponsible.TaskOwner,
                QueryTaskResponsible.Both => ApiTaskResponsible.Both,
                _ => throw new NotSupportedException($"Cannot map {task.Source} to {nameof(ApiTaskSource)}")
            };

            IsResolved = task.IsResolved;
            IsRequired = task.IsRequired;

            ResolvedAt = task.ResolvedAt;
            ResolvedBy = (task.ResolvedBy is not null) ? new ApiPerson(task.ResolvedBy) : null;
            SentBy = new ApiPerson(task.SentBy);

            Properties = new ApiPropertiesCollection(task.Properties);
        }

        public Guid Id { get; }
        public string Title { get; }
        public string Body { get; }
        public string Type { get; }
        public string? SubType { get; }
        public ApiTaskSource Source { get; }
        public ApiTaskResponsible Responsible { get; }
        public bool IsResolved { get; }
        public bool IsRequired { get; }
        public DateTimeOffset? ResolvedAt { get; }
        public ApiPerson? ResolvedBy { get; }
        public ApiPerson SentBy { get; }

        public ApiPropertiesCollection Properties { get;  }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ApiTaskSource { ResourceOwner, TaskOwner }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ApiTaskResponsible { ResourceOwner, TaskOwner, Both }
}
