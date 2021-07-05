using Fusion.Resources.Domain;
using System;
using System.Text.Json.Serialization;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiRequestTask
    {
        public ApiRequestTask(QueryRequestTask task)
        {
            Id = task.Id;
            Title = task.Title;
            Body = task.Body;
            Category = task.Category;
            Type = task.Type;
            SubType = task.SubType;
            Source = task.Source switch
            {
                TaskSource.ResourceOwner => ApiTaskSource.ResourceOwner,
                TaskSource.TaskOwner => ApiTaskSource.TaskOwner,
                _ => throw new NotSupportedException($"Cannot map {task.Source} to {nameof(ApiTaskSource)}")
            };
            Responsible = task.Responsible switch
            {
                TaskResponsible.ResourceOwner => ApiTaskResponsible.ResourceOwner,
                TaskResponsible.TaskOwner => ApiTaskResponsible.TaskOwner,
                TaskResponsible.Both => ApiTaskResponsible.Both,
                _ => throw new NotSupportedException($"Cannot map {task.Source} to {nameof(ApiTaskSource)}")
            };

            IsResolved = task.IsResolved;
            ResolvedAt = task.ResolvedAt;
            ResolvedBy = (task.ResolvedBy is not null) ? new ApiPerson(task.ResolvedBy) : null;

            Properties = new ApiPropertiesCollection(task.Properties);
        }

        public Guid Id { get; }
        public string Title { get; }
        public string Body { get; }
        public string Category { get; }
        public string Type { get; }
        public string? SubType { get; }
        public ApiTaskSource Source { get; }
        public ApiTaskResponsible Responsible { get; }
        public bool IsResolved { get; }
        public DateTimeOffset? ResolvedAt { get; }
        public ApiPerson? ResolvedBy { get; }

        public ApiPropertiesCollection Properties { get;  }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ApiTaskSource { ResourceOwner, TaskOwner }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ApiTaskResponsible { ResourceOwner, TaskOwner, Both }
}
