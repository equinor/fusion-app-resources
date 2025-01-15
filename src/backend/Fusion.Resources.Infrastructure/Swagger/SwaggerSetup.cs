using System.ComponentModel;
using System.Reflection;
using Fusion.AspNetCore.OData;
using Fusion.Resources.Infrastructure.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;

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

                c.CustomSchemaIds(type =>
                {
                    // To fix this swagger gen error
                    // System.InvalidOperationException: Can't use schemaId "$ApiPerson" for type "$Fusion.Services.LineOrg.ApiModels.ApiPerson". The same schemaId is already used for type "$Fusion.Resources.Api.Controllers.ApiPerson"
                    if (type == typeof(Fusion.Services.LineOrg.ApiModels.ApiPerson))
                        return $"{nameof(Fusion.Services.LineOrg.ApiModels.ApiPerson)}_LingOrg";
                    return type.ToString();
                });

                // Only add endpoints that belongs to the version spec
                c.DocInclusionPredicate((version, desc) =>
                {
                    var latestApiVersion = desc.GetApiVersion() ?? new ApiVersion(1, 0);

                    var hasMajorVersion = int.TryParse(version.Split("-")[1].Substring(1), out int majorVersion);

                    if (hasMajorVersion)
                    {
                        return desc.ImplementsMajorVersion(majorVersion); 
                    }

                    if (version == "api-beta")
                    {
                        return latestApiVersion.Status != null;
                    }

                    return true;
                });
                c.OperationFilter<SecurityRequirementsOperationFilter>();
                c.OperationFilter<AddApiVersionParameter>();
                c.SchemaFilter<PatchPropertySchemaFilter>();
                c.SchemaFilter<EnumSchemaFilter>();
                c.OperationFilter<ODataFilterParamSwaggerFilter>();
                c.OperationFilter<ODataExpandParamSwaggerFilter>();
                c.OperationFilter<ODataOrderByParamSwaggerFilter>();
                c.OperationFilter<ODataTopParamSwaggerFilter>();
                c.OperationFilter<ODataSkipParamSwaggerFilter>();
                c.OperationFilter<ODataSearchParamSwaggerFilter>();
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

                string xmlFile = $"{Assembly.GetEntryAssembly()?.GetName().Name}.xml";
                string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                    c.IncludeXmlComments(xmlPath);
            });

            TypeConverterAttribute typeConverterAttribute = new TypeConverterAttribute(typeof(ToStringTypeConverter));
            TypeDescriptor.AddAttributes(typeof(ODataQueryParams), typeConverterAttribute);


            services.AddSwaggerAuthorizationScheme(configuration);


            return services;
        }

        public static void AddSwaggerAuthorizationScheme(this IServiceCollection services, IConfiguration config)
        {
            services.AddAuthentication()
                .AddCookie(SwaggerAuthDefaults.CookieAuthenticationScheme, c =>
                {
                    c.Cookie.Name = ".swagger-auth";
                    c.Cookie.Path = "/swagger";
                })
                .AddOpenIdConnect(SwaggerAuthDefaults.AuthenticationScheme, c =>
                {
                    c.SignInScheme = SwaggerAuthDefaults.CookieAuthenticationScheme;
                    c.Authority = $"https://login.microsoftonline.com/{config.GetValue<string>("Swagger:TenantId")}";
                    c.ClientId = config.GetValue<string>("Swagger:ClientId");
                    c.UsePkce = true;
                    c.RequireHttpsMetadata = true;
                    c.ResponseType = "code";
                    c.ResponseMode = "form_post";
                    c.CallbackPath = "/signin-oidc";

                    // If 'Origin' is null, OIDC throws an exception because "tokens may only be redeemed via cross-origin requests"
                    var defaultBackChannel = new HttpClient();
                    defaultBackChannel.DefaultRequestHeaders.Add("Origin", "fusion.equinor.com");
                    c.Backchannel = defaultBackChannel;
                });
        }

        public static IApplicationBuilder UseFusionSwagger(this IApplicationBuilder app)
        {
            if (FusionSwaggerConfig.UseFusionSwaggerSetup == false)
                return app;

            var instanceConfig = app.ApplicationServices.GetService<IOptions<SwaggerApiConfig>>();
            var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();


            app.MapWhen(SwaggerAuthMiddleware.IsSwaggerUiPath, b =>
            {
                b.UseSwagger();
                b.UseSwaggerUI(c =>
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
                    c.OAuthAdditionalQueryStringParams(new Dictionary<string, string>()
                        { { "resource", configuration.GetValue<string>("Swagger:Resource") } });
                });
            });

            return app;
        }


        public static IEnumerable<ApiVersion> GetSupportedVersions(this ApiDescription apiDescription)
        {
            var thisVersionProp = apiDescription.ActionDescriptor.Properties.FirstOrDefault(prop => (Type)prop.Key == typeof(ApiVersionModel));
            var thisVersionValue = thisVersionProp.Value as ApiVersionModel;

            if (thisVersionValue is not null && thisVersionValue.DeclaredApiVersions.Any())
                return thisVersionValue.DeclaredApiVersions;

            return thisVersionValue?.ImplementedApiVersions ?? new List<ApiVersion>();
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

        public static bool ImplementsMajorVersion(this ApiDescription apiDescription, int version)
        {
            var thisVersionProp = apiDescription.ActionDescriptor.Properties.FirstOrDefault(prop => (Type)prop.Key == typeof(ApiVersionModel));
            var thisVersionValue = thisVersionProp.Value as ApiVersionModel;

            if (thisVersionValue != null && thisVersionValue.DeclaredApiVersions.Any())
            {
                return thisVersionValue.DeclaredApiVersions.Any(v => v.MajorVersion == version && v.Status == null);
            }

            return thisVersionValue?.ImplementedApiVersions.OrderByDescending(p => p).Any(v => v.MajorVersion == version && v.Status == null) == true;
        }
    }
}
