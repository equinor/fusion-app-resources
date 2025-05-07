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
                c.Description = setupBuilder.Description;
            });


            services.AddSwaggerGen(c =>
            {
                setupBuilder.SetupAction?.Invoke(c);

                // To fix this swagger gen error
                // System.InvalidOperationException: Can't use schemaId "$ApiPerson" for type "$Fusion.Services.LineOrg.ApiModels.ApiPerson". The same schemaId is already used for type "$Fusion.Resources.Api.Controllers.ApiPerson"
                // TODO: This is a quick fix, makes the schema models have very long name, for example: Fusion.AspNetCore.Api.PatchProperty`1[Fusion.Resources.Api.Controllers.ApiPropertiesCollection]
                c.CustomSchemaIds(type => type.ToString().Replace("+", "."));

                c.OperationFilter<SecurityRequirementsOperationFilter>();
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

            services.AddSwaggerApiVersioning();
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

            var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();


            app.MapWhen(SwaggerAuthMiddleware.IsSwaggerUiPath, b =>
            {
                b.UseSwagger();
                b.UseSwaggerUI(c =>
                {
                    c.AddApiVersioning(app);
                    c.OAuthAppName(configuration.GetValue<string>("Swagger:OAuthAppName"));
                    c.OAuthClientId(configuration.GetValue<string>("Swagger:ClientId"));
                    c.OAuthRealm(configuration.GetValue<string>("Swagger:TenantId"));
                    c.OAuthAdditionalQueryStringParams(new Dictionary<string, string>()
                        { { "resource", configuration.GetValue<string>("Swagger:Resource") } });
                });
            });

            return app;
        }
    }
}
