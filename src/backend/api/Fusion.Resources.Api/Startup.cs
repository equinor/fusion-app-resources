using Bogus;
using FluentValidation.AspNetCore;
using Fusion.Resources.Domain;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using Fusion.Resources.Api.Middleware;

namespace Fusion.Resources.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Audience = Configuration["AzureAd:ClientId"];
                    options.Authority = "https://login.microsoftonline.com/3aa4a235-b6e2-48d5-9195-7fcf05b459b0";
                    options.SaveToken = true;
                });


            services.AddHttpContextAccessor();
            services.AddSwagger(Configuration);


            // Configure fusion integration
            services.AddFusionIntegration(options =>
            {
                options.AddFusionAuthorization();
                options.AddOrgIntegration();
                options.AddFusionRoles();
                options.AddFusionNotifications(options => options.OriginatingAppKey = "resources");

                options.UseDefaultEndpointResolver(Configuration["FUSION_ENVIRONMENT"] ?? "ci");
                options.UseDefaultTokenProvider(opts =>
                {
                    opts.ClientId = Configuration["AzureAd:ClientId"];
                    opts.ClientSecret = Configuration["AzureAd:ClientSecret"];
                });

                options.ApplicationMode = true;
            });
            services.AddFusionEventHandler("FAP Resources", Configuration["ENVNAME"], (builder) => { });


            services.AddOrgApiClient(Fusion.Integration.Org.OrgConstants.HttpClients.Application, Fusion.Integration.Org.OrgConstants.HttpClients.Delegate);

            services.AddControllers()
                .AddFluentValidation(c =>
                {
                    c.RegisterValidatorsFromAssemblyContaining<Startup>();
                    // Domain project
                    c.RegisterValidatorsFromAssemblyContaining<PersonId>();
                });

            #region Resource services

            services.AddResourceDatabase<Authentication.SqlTokenProvider>(Configuration);
            services.AddResourceDomain();
            services.AddResourceLogic();
            services.AddResourcesApplicationServices();

            services.AddResourcesAuthorizationHandlers();
            services.AddMediatR(typeof(Startup));   // Add notification handlers in api project

            #endregion

            services.AddHealthChecks()
                .AddCheck("liveness", () => HealthCheckResult.Healthy())
                .AddDbContextCheck<Database.ResourcesDbContext>("db", tags: new[] { "ready" });

            services.AddApplicationInsightsTelemetry();

            services.AddSingleton<ChaosMonkey>();
            services.AddCommonLibHttpClient();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseCors(opts => opts
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()
                .WithExposedHeaders("Allow", "x-fusion-retriable"));

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //app.UseMiddleware<Middleware.RequestResponseLoggingMiddleware>();
            app.UseMiddleware<TraceMiddleware>();
            app.UseMiddleware<Middleware.ExceptionMiddleware>();
            app.UseMiddleware<ChaosMonkeyMiddleware>();


            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseResourcesApiSwagger(Configuration);

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();

                // TODO: Remove
                endpoints.MapPost("/release-the-monkey", async (context) =>
                {
                    var monkey = context.RequestServices.GetRequiredService<ChaosMonkey>();

                    var auth = await context.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);

                    if (auth.Succeeded)
                    {
                        monkey.CurrentLevel = monkey.CurrentLevel switch
                        {
                            ChaosMonkey.ChaosLevel.None => ChaosMonkey.ChaosLevel.Intermittent,
                            ChaosMonkey.ChaosLevel.Intermittent => ChaosMonkey.ChaosLevel.Half,
                            ChaosMonkey.ChaosLevel.Half => ChaosMonkey.ChaosLevel.Full,
                            _ => ChaosMonkey.ChaosLevel.None
                        };
                    }
                    await context.Response.WriteAsync($"Changed level to: {monkey.CurrentLevel}");
                });

                // TODO: REMOVE
                endpoints.MapGet("/release-the-monkey", async (context) =>
                {
                    var monkey = context.RequestServices.GetRequiredService<ChaosMonkey>();

                    await context.Response.WriteAsync($"Current level at: {monkey.CurrentLevel}");
                });
            });

            #region Health probes

            app.UseHealthChecks("/_health/liveness", new HealthCheckOptions
            {
                Predicate = r => r.Name.Contains("liveness")
            });
            app.UseHealthChecks("/_health/ready", new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("ready")
            });

            #endregion
        }
    }

    // Leaving this here as it should be removed.
    internal class ChaosMonkey
    {
        public ChaosLevel CurrentLevel { get; set; } = ChaosLevel.None;

        public enum ChaosLevel { None, Intermittent, Half, Full }
    }

    internal class ChaosMonkeyMiddleware
    {
        private readonly RequestDelegate _next;

        public ChaosMonkeyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext, ChaosMonkey thaMonkey)
        {
            if (thaMonkey.CurrentLevel != ChaosMonkey.ChaosLevel.None && ShouldInterruptUrl(httpContext.Request))
            {
                var faker = new Faker();

                var shouldThrow = false;

                switch (thaMonkey.CurrentLevel)
                {
                    case ChaosMonkey.ChaosLevel.Full: shouldThrow = true; break;
                    case ChaosMonkey.ChaosLevel.Half: shouldThrow = faker.Random.Bool(); break;
                    case ChaosMonkey.ChaosLevel.Intermittent: shouldThrow = faker.Random.Number(100) >= 80; break;
                }

                if (shouldThrow)
                {
                    var error = faker.System.Exception();
                    throw error;
                }
            }

            await _next(httpContext);
        }

        private bool ShouldInterruptUrl(HttpRequest request)
        {
            if ((HttpMethods.IsPost(request.Method) || HttpMethods.IsPut(request.Method)) == false)
                return false;

            if (request.Path.StartsWithSegments("/release-the-monkey"))
                return false;

            if (request.Path.StartsWithSegments("/_health"))
                return false;

            return true;
        }
    }
}
