using Fusion.Resources.Api.Middleware;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Controllers
{
    public static class ApiErrors
    {
        public static ActionResult InvalidOperation(Exception error)
        {
            var problem = new ProblemDetails()
            {
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
    }
}
