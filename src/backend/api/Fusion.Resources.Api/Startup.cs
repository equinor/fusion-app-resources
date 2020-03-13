using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bogus;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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

            services.AddSingleton<ChaosMonkey>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseCors(opts => opts
                .AllowAnyOrigin()                
                .AllowAnyMethod()
                .AllowAnyHeader());

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

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

                endpoints.MapPost("/release-the-monkey", async (context) => {
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

                endpoints.MapGet("/release-the-monkey", async (context) => {
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

            if (thaMonkey.CurrentLevel != ChaosMonkey.ChaosLevel.None && !httpContext.Request.Path.StartsWithSegments("/release-the-monkey"))
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
    }
}
