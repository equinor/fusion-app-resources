using Fusion.AspNetCore.OData;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Replace any params of the type ODataQueryParam, with the $-query params.
    /// </summary>
    public class ODataQueryParamSwaggerFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            foreach (var path in swaggerDoc.Paths)
            {
                foreach (var op in path.Value.Operations)
                {
                    var @params = op.Value.Parameters.ToList();

                    var pathParams = @params.Where(p => p.In == ParameterLocation.Query).ToList();

                    var odataQueryParam = pathParams.Where(p => p.Schema != null && p.Schema.Reference != null)
                        .FirstOrDefault(p => p.Schema.Reference.Id == nameof(ODataQueryParams));

                    if (odataQueryParam != null)
                    {
                        op.Value.Parameters.Remove(odataQueryParam);

                        op.Value.Parameters.Add(new OpenApiParameter
                        {
                            Name = "$expand",
                            In = ParameterLocation.Query,
                            Description = "Comma seperated list of properties to include",
                            AllowEmptyValue = true
                        });

                        op.Value.Parameters.Add(new OpenApiParameter
                        {
                            Name = "$filter",
                            In = ParameterLocation.Query,
                            Description = "OData query filter",
                            AllowEmptyValue = true,
                            AllowReserved = true
                        });
                    }
                }
            }
        }
    }
}
