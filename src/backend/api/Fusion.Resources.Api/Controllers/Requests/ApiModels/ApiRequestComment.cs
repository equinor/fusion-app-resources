using Fusion.Resources.Domain;
using System;
using System.Text.Json.Serialization;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiRequestComment
    {
        public ApiRequestComment(QueryRequestComment queryComment)
        {
            Created = queryComment.Created;
            CreatedBy = new ApiPerson(queryComment.CreatedBy);
            Updated = queryComment.Updated;

            UpdatedBy = queryComment.UpdatedBy != null ? new ApiPerson(queryComment.UpdatedBy) : null;
            Content = queryComment.Content;

            if (Enum.TryParse<ApiCommentOrigin>(queryComment.Origin, true, out var result))
                Origin = result;
        }

        public DateTimeOffset Created { get; set; }
        public DateTimeOffset? Updated { get; set; }
        public ApiPerson CreatedBy { get; set; } = null!;
        public ApiPerson? UpdatedBy { get; set; } = null!;

        public string Content { get; set; } = null!;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ApiCommentOrigin Origin { get; set; }
        
        public enum ApiCommentOrigin { Company, Contractor }
    }
}
