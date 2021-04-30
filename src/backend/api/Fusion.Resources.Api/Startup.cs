using FluentValidation.AspNetCore;
using Fusion.Resources.Domain;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Fusion.Resources.Api.Middleware;
using Fusion.Resources.Application.LineOrg;
using Fusion.Integration.Authentication;
using Fusion.Resources.Api.Authentication;

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

            services.AddEventSubscription(setup =>
            {
                setup.ConnectionString = Configuration.GetConnectionString("ServiceBus");
                setup.TopicPath = "resources-sub";
                setup.Source = "FAP Resources";
            });

            services.AddApiVersioning(s =>
            {
                s.ReportApiVersions = true;
                s.AssumeDefaultVersionWhenUnspecified = true;
                s.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
                s.ApiVersionReader = new Fusion.AspNetCore.Mvc.Versioning.HeaderOrQueryVersionReader("api-version");
            });

            services.AddHttpContextAccessor();
            services.AddSwagger(Configuration);

            


            // Configure fusion integration
            services.AddFusionIntegration(options =>
            {
                try { options.AddProfileSync<FusionEvents.ProfileSyncHandler>(); } 
                catch { /* Shitfix untill fixed in integration lib. Throws when added multitple times, which is done in the integration tests. (static bool flag) */ }

                options.AddFusionAuthorization();
                options.AddOrgIntegration();
                options.AddFusionRoles();
                options.AddFusionNotifications(opts => opts.OriginatingAppKey = "resources");

                options.UseDefaultEndpointResolver(Configuration["FUSION_ENVIRONMENT"] ?? "ci");
                options.UseDefaultTokenProvider(opts =>
                {
                    opts.ClientId = Configuration["AzureAd:ClientId"];
                    opts.ClientSecret = Configuration["AzureAd:ClientSecret"];
                });

                options.ApplicationMode = true;
            });
            services.AddFusionEventHandler("FAP Resources", Configuration["ENVNAME"], (builder) => { });

            // Add custom claims provider, to sort delegated responsibilities
            services.AddScoped<ILocalClaimsTransformation, DelegatedResourceOwnerClaimsTransformer>();


            services.AddOrgApiClient(Fusion.Integration.Org.OrgConstants.HttpClients.Application, Fusion.Integration.Org.OrgConstants.HttpClients.Delegate);

            services.AddControllers()
                .AddFluentValidation(c =>
                {
                    c.RegisterValidatorsFromAssemblyContaining<Startup>();
                    // Domain project
                    c.RegisterValidatorsFromAssemblyContaining<PersonId>();
                    // Logic project, where ResourceAllocationRequest having validators
                    c.RegisterValidatorsFromAssemblyContaining<Logic.Commands.ResourceAllocationRequest>();
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

            services.AddCommonLibHttpClient();
            services.AddLineOrgHttpClient();

            services.UseLineOrgIntegration();

            services.AddMemoryCache();
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

            //app.UseMiddleware<RequestResponseLoggingMiddleware>();
            app.UseMiddleware<TraceMiddleware>();
            app.UseMiddleware<ExceptionMiddleware>();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseResourcesApiSwagger(Configuration);

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();

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

}
