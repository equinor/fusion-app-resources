using Fusion.Resources.Api.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class StartupExtensions
    {
        public static IServiceCollection AddSwagger(this IServiceCollection services, IConfiguration config)
        {
            var tenantId = config.GetValue<string>("Swagger:TenantId");

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("resources-api-v1", new OpenApiInfo { Title = "Fusion Resources API", Version = "1.0" });
                c.OperationFilter<SecurityRequirementsOperationFilter>();
                c.DocumentFilter<SwaggerComplexRouteShitfix>();
                c.MapType<ProjectIdentifier>(() => new OpenApiSchema { Type = typeof(string).Name  });
                c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme()
                {
                    In = ParameterLocation.Header,
                    Name = "Authorization",
                    Description = "Azure Active Directory",
                    Flows = new OpenApiOAuthFlows()
                    {
                        Implicit = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri($"https://login.microsoftonline.com/{tenantId}/oauth2/authorize"),
                            TokenUrl = new Uri($"https://login.microsoftonline.com/{tenantId}/oauth2/token"),
                        }
                    },
                    Type = SecuritySchemeType.OAuth2
                });

                var securityRequirement = new OpenApiSecurityRequirement();
                securityRequirement.Add(new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Id = "oauth2",
                        Type = ReferenceType.SecurityScheme
                    }
                }, new List<string>());

                c.AddSecurityRequirement(securityRequirement);
            });

            return services;
        }

        public static IApplicationBuilder UseResourcesApiSwagger(this IApplicationBuilder app, IConfiguration configuration)
        {
            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/resources-api-v1/swagger.json", "Fusion Resources API 1.0");

                c.OAuthAppName(configuration.GetValue<string>("Swagger:OAuthAppName"));
                c.OAuthClientId(configuration.GetValue<string>("Swagger:ClientId"));
                c.OAuthRealm(configuration.GetValue<string>("Swagger:TenantId"));
                c.OAuthAdditionalQueryStringParams(new Dictionary<string, string>() { { "resource", configuration.GetValue<string>("Swagger:Resource") } });
            });

            return app;
        }

    }



    public class SwaggerComplexRouteShitfix : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var projectIdentifier = typeof(ProjectIdentifier);
            var props = projectIdentifier.GetProperties().Select(p => p.Name);

            foreach (var path in swaggerDoc.Paths)
            {
                foreach (var op in path.Value.Operations)
                {
                    var @params = op.Value.Parameters.ToList();

                    var pathParams = @params.Where(p => p.In == ParameterLocation.Path).ToList();
                    

                    if (props.All(p => pathParams.Any(pp => pp.Name == p)))
                    {
                        var paramsToRemove = pathParams.Where(pp => props.Contains(pp.Name)).ToList();

                        foreach (var toRemove in paramsToRemove)
                        {
                            op.Value.Parameters.Remove(toRemove);
                        }
                    }
                }
            }
        }
    }
}
