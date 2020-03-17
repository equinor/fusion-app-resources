using Fusion.Integration;
using Fusion.Integration.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading.Tasks;

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

                options.UseDefaultEndpointResolver("ci");
                //options.UseEndpointResolver<LocalEndpointResolver>();
                options.UseDefaultTokenProvider(opts =>
                {
                    opts.ClientId = Configuration["AzureAd:ClientId"];
                    opts.ClientSecret = Configuration["AzureAd:ClientSecret"];
                });

                options.ApplicationMode = true;
            });

            services.AddOrgApiClient(Fusion.Integration.Org.OrgConstants.HttpClients.Application, Fusion.Integration.Org.OrgConstants.HttpClients.Delegate);



            services.AddControllers();

            #region Resource services

            services.AddResourceDatabase<Authentication.SqlTokenProvider>(Configuration);
            services.AddResourceDomain();
            services.AddResourceLogic();
            services.AddResourcesApplicationServices();

            #endregion

            services.AddHealthChecks()
                .AddCheck("liveness", () => HealthCheckResult.Healthy())
                .AddDbContextCheck<Database.ResourcesDbContext>("db", tags: new[] { "ready" });

            services.AddApplicationInsightsTelemetry();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseCors(opts => opts
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());

            //if (env.IsDevelopment())
            //{
            app.UseDeveloperExceptionPage();
            //}

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

        /// <summary>
        /// Change 
        ///     o.UseDefaultEndpointResolver("ci") --> o.UseEndpointResolver<LocalEndpointResolver>() 
        /// in the Fusion Integration section to run Fusion services locally and connect to them from Query.
        /// </summary>
        private class LocalEndpointResolver : IFusionEndpointResolver
        {
            public Task<string> ResolveEndpointAsync(FusionEndpoint endpoint)
            {
                switch (endpoint)
                {
                    case FusionEndpoint.People:
                        return Task.FromResult("https://pro-s-people-pr-1669.azurewebsites.net");
                    case FusionEndpoint.Mail:
                        return Task.FromResult("https://pro-s-mail-ci.azurewebsites.net");
                    case FusionEndpoint.ProOrganisation:
                        return Task.FromResult("https://pro-s-org-ci.azurewebsites.net");
                    case FusionEndpoint.Context:
                        return Task.FromResult("https://pro-s-context-ci.azurewebsites.net");
                    default:
                        throw new Exception("Endpoint not supported");
                }
            }
            public Task<string> ResolveResource()
            {
                return Task.FromResult("5a842df8-3238-415d-b168-9f16a6a6031b"); //Statoil ProView Test app id
            }
        }
    }
}
