using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Fusion.Resources.Api.Middleware
{
    public class ApiProblem
    {
        public ApiProblem(HttpStatusCode code, string title, string details)
        {
            Status = (int)code;
            Title = title;
            Details = details;

            Error = new ApiError(code.ToString(), details);
        }

        public string Title { get; set; }
        public string Type { get; set; } = "about:blank";
        public int Status { get; set; }
        public string Details { get; set; }
        public string? Instance { get; set; }

        public ApiError Error { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? StackTrace { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? ExceptionMessage { get; set; }


        public class ApiError
        {
            public ApiError(string code, string message)
            {
                Code = code;
                Message = message;
            }

            public string Code { get; }
            public string Message { get; }
        }
    }
}
