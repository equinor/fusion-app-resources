using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{
    public class AddApiVersionParameter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var swaggerDocName = context.DocumentName;
            var hasMajorVersion = int.TryParse(swaggerDocName.Split("-")[1].Substring(1), out int majorVersion);


            var declaredVersions = context.ApiDescription.GetSupportedVersions();

            var version = declaredVersions.OrderByDescending(c => c).FirstOrDefault(v => v.MajorVersion == majorVersion);
            if (version is null) { version = declaredVersions.FirstOrDefault() ?? new ApiVersion(1, 0); }


            var apiVersionParam = new OpenApiParameter()
            {
                Name = "api-version",
                Required = true,
                In = ParameterLocation.Query
            };

            apiVersionParam.Example = new OpenApiString(version!.ToString());
            operation.Parameters.Add(apiVersionParam);


            operation.Description += "Supported api version: " + string.Join(", ", declaredVersions.Select(a => a.ToString()));

            // Check if the endpoint has newer version
            if (declaredVersions.Where(v => v > version).Any())
            {
                operation.Deprecated = true;
                operation.Description += "\n\n> API HAS NEWER VERSION\n\n";
            }


        }
    }
}
