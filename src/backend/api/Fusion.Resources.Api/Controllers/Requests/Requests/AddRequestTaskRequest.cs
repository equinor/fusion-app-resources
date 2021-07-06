using FluentValidation;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fusion.Resources.Api.Controllers.Requests
{
    public class AddRequestTaskRequest
    {
        public string Title { get; set; } = null!;
        public string Body { get; set; } = null!;
        public string Type { get; set; } = null!;
        public string? SubType { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ApiTaskSource Source { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ApiTaskResponsible Responsible { get; set; }

        [JsonConverter(typeof(Json.DictionaryStringObjectJsonConverter))]
        public Dictionary<string, object>? Properties { get; set; }

        public class Validator : AbstractValidator<AddRequestTaskRequest>
        {
            public Validator()
            {
                RuleFor(r => r.Title).NotEmpty();
                RuleFor(r => r.Body).NotEmpty();
                RuleFor(r => r.Type).NotEmpty();
            }
        }
    }
}
