using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.Extensions.DependencyInjection
{
    public class AddApiVersionParameter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var version = context.ApiDescription.GetApiVersion() ?? new AspNetCore.Mvc.ApiVersion(1, 0);

            var apiVersionParam = new OpenApiParameter()
            {
                Name = "api-version",
                Required = true,
                In = ParameterLocation.Query
            };
            apiVersionParam.Example = new OpenApiString(version.ToString());

            operation.Parameters.Add(apiVersionParam);
                
        }
    }
}
