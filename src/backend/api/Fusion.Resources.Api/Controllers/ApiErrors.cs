using Fusion.Integration;
using Fusion.Resources.Api.Middleware;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Fusion.Resources.Api.Controllers
{
    public static class ApiErrors
    {
        private const string rfcProblemDetails = "https://tools.ietf.org/html/rfc7231#section-6.5.1";

        public static ActionResult InvalidOperation(Exception error)
        {
            var problem = new ProblemDetails()
            {
                Type = rfcProblemDetails,
                Detail = error.Message,
                Title = "Invalid Operation",
                Status = (int)System.Net.HttpStatusCode.BadRequest
            };
            problem.Extensions.Add("error", new ApiProblem.ApiError(error.GetType().Name, error.Message));

            return new ObjectResult(problem)
            {
                StatusCode = problem.Status
            };
        }

        internal static ActionResult InvalidPageSize(string message)
        {
            var problem = new ProblemDetails
            {
                Type = rfcProblemDetails,
                Detail = message,
                Title = "Invalid page size",
                Status = (int)System.Net.HttpStatusCode.BadRequest
            };

            return new ObjectResult(problem)
            {
                StatusCode = problem.Status
            };
        }

        internal static ActionResult NotFound(string message)
        {
            var problem = new ProblemDetails
            {
                Type = rfcProblemDetails,
                Detail = message,
                Title = "Resource not found",
                Status = (int)System.Net.HttpStatusCode.NotFound
            };

            return new ObjectResult(problem)
            {
                StatusCode = problem.Status
            };
        }

        internal static ActionResult FailedFusionRequest(FusionEndpoint endpoint, string message)
        {
            var problem = new ProblemDetails
            {
                Type = rfcProblemDetails,
                Detail = $"Error invoking endpoint on service '{endpoint}': {message}",
                Title = "Fusion service request failed",
                Status = (int)System.Net.HttpStatusCode.FailedDependency
            };

            return new ObjectResult(problem)
            {
                StatusCode = problem.Status
            };
        }
    }
}
