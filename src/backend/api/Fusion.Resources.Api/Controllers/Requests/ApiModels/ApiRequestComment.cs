using System;
using System.Text.Json.Serialization;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiRequestComment
    {
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset? Updated { get; set; }
        public ApiPerson CreatedBy { get; set; } = null!;
        public ApiPerson UpdatedBy { get; set; } = null!;

        public string Content { get; set; } = null!;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ApiCommentOrigin Origin { get; set; }


        public enum ApiCommentOrigin { Company, Contractor }
    }
}
