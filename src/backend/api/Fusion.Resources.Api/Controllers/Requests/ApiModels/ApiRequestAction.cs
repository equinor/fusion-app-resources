using Fusion.Resources.Domain;
using System;
using System.Text.Json.Serialization;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiRequestAction
    {
        public ApiRequestAction(QueryRequestAction action)
        {
            Id = action.Id;
            Title = action.Title;
            Body = action.Body;
            Type = action.Type;
            SubType = action.SubType;
            Source = action.Source switch
            {
                QueryTaskSource.ResourceOwner => ApiTaskSource.ResourceOwner,
                QueryTaskSource.TaskOwner => ApiTaskSource.TaskOwner,
                _ => throw new NotSupportedException($"Cannot map {action.Source} to {nameof(ApiTaskSource)}")
            };
            Responsible = action.Responsible switch
            {
                QueryTaskResponsible.ResourceOwner => ApiTaskResponsible.ResourceOwner,
                QueryTaskResponsible.TaskOwner => ApiTaskResponsible.TaskOwner,
                QueryTaskResponsible.Both => ApiTaskResponsible.Both,
                _ => throw new NotSupportedException($"Cannot map {action.Source} to {nameof(ApiTaskSource)}")
            };

            IsResolved = action.IsResolved;
            IsRequired = action.IsRequired;

            ResolvedAt = action.ResolvedAt;
            ResolvedBy = (action.ResolvedBy is not null) ? new ApiPerson(action.ResolvedBy) : null;
            SentBy = (action.SentBy is not null) ? new ApiPerson(action.SentBy) : null;

            DueDate = action.DueDate;
            AssignedTo = (action.AssignedTo is not null) ? new ApiPerson(action.AssignedTo) : null;

            Properties = new ApiPropertiesCollection(action.Properties);
        }

        public Guid Id { get; }
        public string Title { get; }
        public string? Body { get; }
        public string Type { get; }
        public string? SubType { get; }
        public ApiTaskSource Source { get; }
        public ApiTaskResponsible Responsible { get; }
        public bool IsResolved { get; }
        public bool IsRequired { get; }
        public DateTimeOffset? ResolvedAt { get; }
        public ApiPerson? ResolvedBy { get; }
        public ApiPerson? SentBy { get; }
        public DateTime? DueDate { get; }
        public ApiPerson? AssignedTo { get; }
        public ApiPropertiesCollection Properties { get;  }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ApiTaskSource { ResourceOwner, TaskOwner }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ApiTaskResponsible { ResourceOwner, TaskOwner, Both }
}
