using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.DependencyInjection;

namespace Fusion.Testing.Mocks.LineOrgService.Api
{
    internal class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers()
                .AddNewtonsoftJson();

            services.AddApiVersioning(s =>
            {
                s.ReportApiVersions = true;
                s.AssumeDefaultVersionWhenUnspecified = true;
                s.DefaultApiVersion = new ApiVersion(1, 0);
                s.ApiVersionReader = ApiVersionReader.Combine(new HeaderApiVersionReader(new[] { "api-version" }), new QueryStringApiVersionReader()); // The default is api-version
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();
            app.UseApiVersioning();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
