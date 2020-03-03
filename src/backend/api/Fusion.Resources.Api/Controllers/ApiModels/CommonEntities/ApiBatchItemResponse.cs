using System.Net;
using System.Text.Json.Serialization;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiBatchItemResponse<T>
    {
        public ApiBatchItemResponse(HttpStatusCode code, string? message = null)
        {
            Value = default(T)!;
            Code = code;
            Message = message;
        }
        public ApiBatchItemResponse(T item, HttpStatusCode code, string? message = null)
        {
            Value = item;
            Code = code;
            Message = message;
        }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public HttpStatusCode Code { get; set; }
        public string? Message { get; set; }
        public T Value { get; set; }
    }


}
