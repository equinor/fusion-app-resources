using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Linq;

namespace Fusion.Resources.Api.Swagger
{
    /// <summary>
    /// Swagger document filter to post-process all the routes, and remove required path params found in controller action, but not in the route. 
    /// This supports the scenario where one action support multiple routes, where some params can be null.
    /// </summary>
    public class OptionalRouteParamFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            foreach (var path in swaggerDoc.Paths)
            {
                var route = Microsoft.AspNetCore.Routing.Template.TemplateParser.Parse(path.Key);
                var parameters = route.Parameters.Where(p => p.IsParameter).Select(p => p.Name);

                foreach (var operation in path.Value.Operations.Values)
                {
                    var routeParamsToRemove = operation.Parameters
                        .Where(p => p.In == ParameterLocation.Path)
                        .Where(p => !parameters.Any(routeParam => string.Equals(routeParam, p.Name, StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                    routeParamsToRemove.ForEach(r => operation.Parameters.Remove(r));
                }
            }

        }
    }
}
