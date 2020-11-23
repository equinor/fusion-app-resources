using System;
using System.Net;

namespace Fusion.Resources.Functions.Integration
{
    public class ApiError : Exception
    {
        public ApiError(string url, HttpStatusCode statusCode, string body, string message) : base($"{message}. Status code: {statusCode}, Body: {body.Substring(0, 500)}")
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
