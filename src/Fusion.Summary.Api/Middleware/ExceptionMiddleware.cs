using System.Net;
using Fusion.AspNetCore;
using Microsoft.ApplicationInsights;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Fusion.Summary.Api.Middleware;

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
        catch (Exception ex) when (ex is ODataException)
        {
            var responseData = new ApiProblem(HttpStatusCode.BadRequest, "Invalid query expression", ex.Message);

            await WriteResponseAsync(httpContext, responseData);
        }
        catch (Exception ex)
        {
            if (IsNotTransientException(ex))
                httpContext.Response.Headers["x-fusion-retriable"] = "false";

            TraceException(telemetryClient, ex);

            var responseData = new ApiProblem(HttpStatusCode.InternalServerError, "Unhandled error", ex.Message);

            if (httpContext.User.IsInRole("Fusion.Developer") || httpContext.User.IsInRole("ProView.Admin.DevOps") ||
                webHost.IsDevelopment() || httpContext.Request.Host.Host == "localhost")
            {
                responseData.ExceptionMessage = ex.Message;
                responseData.StackTrace = ex.ToString()
                    .Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            await WriteResponseAsync(httpContext, responseData);
        }
    }

    private async Task WriteResponseAsync(HttpContext httpContext, ApiProblem responseData)
    {
        var formater = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
        var errorString = JsonConvert.SerializeObject(responseData, Formatting.Indented, formater);

        httpContext.Response.ContentType = "application/json";
        httpContext.Response.ContentLength = errorString.Length;
        httpContext.Response.StatusCode = responseData.Status;

        await httpContext.Response.WriteAsync(errorString);
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