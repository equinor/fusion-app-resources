using System;
using System.Text.Json.Serialization;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiRequestComment
    {
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset? Updated { get; set; }
        public ApiPerson CreatedBy { get; set; }
        public ApiPerson UpdatedBy { get; set; }

        public string Content { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ApiCommentOrigin Origin { get; set; }


        public enum ApiCommentOrigin { Company, Contractor }
    }
}
