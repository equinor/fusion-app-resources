using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Integration;
using Newtonsoft.Json;

namespace Fusion.Resources.Api.Middleware
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly TelemetryClient telemetryClient;

        public RequestResponseLoggingMiddleware(RequestDelegate next, TelemetryClient telemetryClient)
        {
            _next = next;
            this.telemetryClient = telemetryClient;
        }

        public async Task Invoke(HttpContext context)
        {
            //First, get the incoming request
            var request = await FormatRequest(context.Request);

            //Copy a pointer to the original response body stream
            var originalBodyStream = context.Response.Body;

            //Create a new memory stream...
            using (var responseBody = new MemoryStream())
            {
                context.Response.Body = responseBody;

                //Continue down the Middleware pipeline, eventually returning to this class
                await _next(context);

                // Seek to start of response stream, so either copy operation works.
                responseBody.Seek(0, SeekOrigin.Begin);

                if (context.Response.StatusCode > 300)
                {
                    //Format the response from the server
                    var response = await FormatResponse(context.Response);

                    telemetryClient.TrackTrace("Request: " + request);
                    telemetryClient.TrackTrace("Response: " + response);
                    telemetryClient.TrackTrace(string.Join(",\n", context.User.Claims.Select(c => $"{c.Type}:{c.Value}")));

                    var profile = context.GetProfile();
                    if (profile != null)
                        telemetryClient.TrackTrace(JsonConvert.SerializeObject(profile, Formatting.Indented));
                }

                //Copy the contents of the new memory stream (which contains the response) to the original stream, which is then returned to the client.
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }

        private async Task<string> FormatRequest(HttpRequest request)
        {
            var body = request.Body;

            request.EnableBuffering();

            var buffer = new byte[Convert.ToInt32(request.ContentLength)];
            await request.Body.ReadAsync(buffer, 0, buffer.Length);
            var bodyAsText = Encoding.UTF8.GetString(buffer);
            
            request.Body = body;

            return $"{request.Scheme} {request.Host}{request.Path} {request.QueryString} {bodyAsText}";
        }

        private async Task<string> FormatResponse(HttpResponse response)
        {
            //We need to read the response stream from the beginning...
            

            //...and copy it into a string
            string text = await new StreamReader(response.Body).ReadToEndAsync();

            //We need to reset the reader for the response so that the client can read it.
            response.Body.Seek(0, SeekOrigin.Begin);

            //Return the string for the response, including the status code (e.g. 200, 404, 401, etc.)
            return $"{response.StatusCode}: {text}";
        }
    }
}
