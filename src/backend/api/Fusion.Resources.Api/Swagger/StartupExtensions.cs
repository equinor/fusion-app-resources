using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
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
}
