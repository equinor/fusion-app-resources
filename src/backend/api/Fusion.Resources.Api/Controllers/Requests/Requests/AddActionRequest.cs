using FluentValidation;
using Fusion.Resources.Domain;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fusion.Resources.Api.Controllers.Requests
{
    public class AddActionRequest
    {
        public string Title { get; set; } = null!;
        public string? Body { get; set; } = null!;
        public string Type { get; set; } = null!;
        public string? SubType { get; set; }

        public Guid? AssignedToId { get; set; }
        public DateTime? DueDate { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ApiTaskSource Source { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ApiTaskResponsible Responsible { get; set; }

        [JsonConverter(typeof(Json.DictionaryStringObjectJsonConverter))]
        public Dictionary<string, object>? Properties { get; set; }

        public bool IsRequired { get; set; } = false;

        public class Validator : AbstractValidator<AddActionRequest>
        {
            public Validator(IProfileService profileService)
            {
                RuleFor(r => r.Title).NotEmpty().MaximumLength(100);
                RuleFor(r => r.Body).MaximumLength(2000);
                RuleFor(r => r.Type).NotEmpty().MaximumLength(60);
                RuleFor(r => r.SubType).MaximumLength(60);

                RuleFor(x => x.AssignedToId)
                   .MustAsync(async (assignedToId, cancelToken) =>
                   {
                       var assigned = await profileService.EnsurePersonAsync(assignedToId!.Value);
                       return assigned != null;
                   })
                   .When(x => x.AssignedToId.HasValue);
            }
        }
    }
}
