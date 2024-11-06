using System.Net;

namespace Fusion.Resources.Functions.Common.Integration.Errors
{
    public class ApiError : Exception
    {
        public ApiError(string url, HttpStatusCode statusCode, string body, string message) :
            base($"{message}. Status code: {statusCode}, Body: {(body.Length > 500 ? body[..500] : body)}")
        {
            Url = url;
            StatusCode = statusCode;
            Body = body;
        }

        public string Url { get; }

        public HttpStatusCode StatusCode { get; }

        public string Body { get; }
    }
}