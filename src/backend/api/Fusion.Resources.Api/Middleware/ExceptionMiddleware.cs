using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Microsoft.ApplicationInsights;

namespace Fusion.Resources.Api.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        // IMyScopedService is injected into Invoke
        public async Task Invoke(HttpContext httpContext, IWebHostEnvironment webHost, TelemetryClient telemetryClient)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                
                if (IsNotTransientException(ex))
                    httpContext.Response.Headers["x-fusion-retriable"] = "false";

                TraceException(telemetryClient, ex);

                var responseData = new ApiProblem(HttpStatusCode.InternalServerError, "Unhandle error", ex.Message);

                if (httpContext.User.IsInRole("Fusion.Developer") || httpContext.User.IsInRole("ProView.Admin.DevOps") || webHost.IsDevelopment() || httpContext.Request.Host.Host == "localhost")
                {
                    responseData.ExceptionMessage = ex.Message;
                    responseData.StackTrace = ex.ToString().Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
                }


                var formater = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
                var errorString = JsonConvert.SerializeObject(responseData, Formatting.Indented, formater);

                var responseFeature = httpContext.Features.Get<IHttpResponseBodyFeature>();

                httpContext.Request.ContentType = "application/json";
                httpContext.Response.ContentLength = errorString.Length;

                var writer = new StreamWriter(responseFeature.Stream);
                await writer.WriteAsync(errorString);
                await writer.FlushAsync();
            }
        }

        private bool IsNotTransientException(Exception ex)
        {
            if (ex is NullReferenceException 
                || ex is ArgumentNullException
                || ex is ArgumentException 
                || ex is InvalidOperationException)
            {
                return true;
            }

            return false;
        }
    
        private void TraceException(TelemetryClient telemetryClient, Exception ex)
        {
            if (ex.Data["ai-tracked"] == null)
                telemetryClient.TrackException(ex);
        }
    }


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
