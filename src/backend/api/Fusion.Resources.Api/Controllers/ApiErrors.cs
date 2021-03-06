﻿using Fusion.Integration;
using Fusion.Resources.Api.Middleware;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace Fusion.Resources.Api.Controllers
{
    public static class ApiErrors
    {
        private const string rfcProblemDetails = "https://tools.ietf.org/html/rfc7231#section-6.5.1";

        public static ActionResult InvalidOperation(FluentValidation.ValidationException error)
        {
            var problem = new ProblemDetails()
            {
                Type = rfcProblemDetails,
                Detail = error.Message,
                Title = "Invalid Operation",
                Status = (int)System.Net.HttpStatusCode.BadRequest
            };
            problem.Extensions.Add("error", new ApiProblem.ApiError(error.GetType().Name, error.Message));

            // Validation errors
            var propertyErrors = error.Errors.GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage));

            problem.Extensions.Add("errors", propertyErrors);

            return new ObjectResult(problem)
            {
                StatusCode = problem.Status
            };
        }

        public static ActionResult InvalidOperation(string code, string message)
        {
            var problem = new ProblemDetails()
            {
                Type = rfcProblemDetails,
                Detail = message,
                Title = "Invalid Operation",
                Status = (int)System.Net.HttpStatusCode.BadRequest
            };
            problem.Extensions.Add("error", new ApiProblem.ApiError(code, message));

            return new ObjectResult(problem)
            {
                StatusCode = problem.Status
            };
        }

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

        public static ActionResult InvalidInput(string message)
        {
            var problem = new ProblemDetails()
            {
                Type = rfcProblemDetails,
                Detail = message,
                Title = "Invalid Operation",
                Status = (int)System.Net.HttpStatusCode.BadRequest
            };
            problem.Extensions.Add("error", new ApiProblem.ApiError("InvalidInput", message));

            return new ObjectResult(problem)
            {
                StatusCode = problem.Status
            };
        }

        public static ActionResult MissingInput(string paramName, string message)
        {
            var problem = new ProblemDetails()
            {
                Type = rfcProblemDetails,
                Detail = message,
                Title = "Invalid Operation",
                Status = (int)System.Net.HttpStatusCode.BadRequest
            };
            problem.Extensions.Add("error", new ApiProblem.MissingPropertyError(paramName, "MissingInput", message));

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

        internal static ActionResult NotFound(string message, string? resourcePath = null)
        {
            var problem = new ProblemDetails
            {
                Type = rfcProblemDetails,
                Detail = message,
                Title = "Resource not found",
                Instance = resourcePath,
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

        internal static ActionResult FailedDependency(string identifier, string message)
        {
            var problem = new ProblemDetails
            {
                Type = rfcProblemDetails,
                Detail = $"Error invoking endpoint on service '{identifier}': {message}",
                Title = "External dependency request failed",
                Status = (int)System.Net.HttpStatusCode.FailedDependency
            };
            problem.Extensions.Add("error", new ApiProblem.ApiError("FailedDependency", message));

            return new ObjectResult(problem)
            {
                StatusCode = problem.Status
            };
        }
    }
}
