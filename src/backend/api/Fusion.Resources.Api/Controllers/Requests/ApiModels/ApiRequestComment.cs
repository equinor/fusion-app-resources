using System;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiRequestComment
    {
        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
        public ApiPerson CreatedBy { get; set; }
        public ApiPerson UpdatedBy { get; set; }

        public string Content { get; set; }

        public ApiCommentOrigin Origin { get; set; }


        public enum ApiCommentOrigin { Company, Contractor }
    }
}
