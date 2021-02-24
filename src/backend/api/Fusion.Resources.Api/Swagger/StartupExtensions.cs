using Fusion.Resources.Api.Controllers;
using Fusion.Resources.Api.Swagger;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class StartupExtensions
    {
        public static IServiceCollection AddSwagger(this IServiceCollection services, IConfiguration config)
        {

            services.AddSwagger(config, "Fusion Resources API", swagger => swagger
                .AddApiVersion(1)

                // When promoting endpoints, add new version to update the version dropdown 
                //.AddApiVersion(2)

                .AddApiPreview()
                .ForceStringConverter<ProjectIdentifier>()
                .ConfigureSwaggerGen(s =>
                {
                    s.MapType<ProjectIdentifier>(() => new OpenApiSchema { Type = "string", Description = "Org project id or context id" });

                    s.DocumentFilter<OptionalRouteParamFilter>();
                }));

            return services;
        }

        public static IApplicationBuilder UseResourcesApiSwagger(this IApplicationBuilder app, IConfiguration configuration)
        {
            app.UseFusionSwagger();

            return app;
        }

    }

}
