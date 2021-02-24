using Fusion.AspNetCore.OData;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{

    public static class SwaggerSetup
    {
        public static IServiceCollection AddSwagger(this IServiceCollection services, IConfiguration configuration, string title, Action<FusionSwaggerConfig> fusionSwaggerSetup)
        {
            var tenantId = configuration.GetValue<string>("Swagger:TenantId");

            var setupBuilder = new FusionSwaggerConfig();
            fusionSwaggerSetup(setupBuilder);

            services.Configure<SwaggerApiConfig>(c =>
            {
                c.Title = title;
                c.EnabledVersions.AddRange(setupBuilder.EnabledVersions);
                c.EnablePreview = setupBuilder.AddPreviewEndpoints;
            });


            services.AddSwaggerGen(c =>
            {

                foreach (var version in setupBuilder.EnabledVersions)
                    c.SwaggerDoc($"api-v{version}", new OpenApiInfo { Title = title, Version = $"{version}.0" });

                if (setupBuilder.AddPreviewEndpoints)
                    c.SwaggerDoc($"api-beta", new OpenApiInfo { Title = title, Version = $"Previews" });

                setupBuilder.SetupAction?.Invoke(c);

                // Only add endpoints that belongs to the version spec
                c.DocInclusionPredicate((version, desc) =>
                {
                    var v = desc.GetApiVersion() ?? new ApiVersion(1, 0);

                    if (int.TryParse(version.Split("-")[1].Substring(1), out int major))
                    {
                        return v.MajorVersion == major && v.Status == null;
                    }

                    if (version == "api-beta")
                    {
                        return v.Status != null;
                    }

                    return true;
                });
                c.OperationFilter<SecurityRequirementsOperationFilter>();
                c.OperationFilter<AddApiVersionParameter>();

                c.DocumentFilter<ODataQueryParamSwaggerFilter>();

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

            TypeConverterAttribute typeConverterAttribute = new TypeConverterAttribute(typeof(ToStringTypeConverter));
            TypeDescriptor.AddAttributes(typeof(ODataQueryParams), typeConverterAttribute);

            return services;
        }

        public static IApplicationBuilder UseFusionSwagger(this IApplicationBuilder app)
        {
            if (FusionSwaggerConfig.UseFusionSwaggerSetup == false)
                return app;

            var instanceConfig = app.ApplicationServices.GetService<IOptions<SwaggerApiConfig>>();
            var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                if (instanceConfig != null)
                {
                    foreach (var version in instanceConfig.Value.EnabledVersions)
                        c.SwaggerEndpoint($"/swagger/api-v{version}/swagger.json", $"v{version}.0");

                    if (instanceConfig.Value.EnablePreview)
                        c.SwaggerEndpoint($"/swagger/api-beta/swagger.json", $"Previews");
                }

                c.OAuthAppName(configuration.GetValue<string>("Swagger:OAuthAppName"));
                c.OAuthClientId(configuration.GetValue<string>("Swagger:ClientId"));
                c.OAuthRealm(configuration.GetValue<string>("Swagger:TenantId"));
                c.OAuthAdditionalQueryStringParams(new Dictionary<string, string>() { { "resource", configuration.GetValue<string>("Swagger:Resource") } });
            });

            return app;
        }

        public static ApiVersion? GetApiVersion(this ApiDescription apiDescription)
        {
            var thisVersionProp = apiDescription.ActionDescriptor.Properties.FirstOrDefault(prop => (Type)prop.Key == typeof(ApiVersionModel));
            var thisVersionValue = thisVersionProp.Value as ApiVersionModel;
            var thisDeclaredVersion = thisVersionValue?.DeclaredApiVersions.OrderByDescending(p => p).FirstOrDefault();

            if (thisDeclaredVersion != null)
                return thisDeclaredVersion;

            return thisVersionValue?.ImplementedApiVersions.OrderByDescending(p => p).FirstOrDefault();
        }




    }
}
