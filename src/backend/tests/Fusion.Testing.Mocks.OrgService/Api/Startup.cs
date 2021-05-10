using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Fusion.Testing.Mocks.OrgService.Api
{
    public class Startup
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
                s.DefaultApiVersion = new ApiVersion(2, 0);
                s.ApiVersionReader = ApiVersionReader.Combine(new HeaderApiVersionReader(new[] { "api-version" }), new QueryStringApiVersionReader()); // The default is api-version
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseMiddleware<RequestLoggingMiddleware>();
            app.UseMiddleware<RequestMockMiddleware>();
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }

    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            await OrgServiceMock.semaphore.WaitAsync();

            try
            {
                //First, get the incoming request
                context.Request.EnableBuffering();

                using (var membuffer = new MemoryStream())
                {
                    await context.Request.Body.CopyToAsync(membuffer);

                    using (var reader = new StreamReader(membuffer))
                    {
                        membuffer.Seek(0, SeekOrigin.Begin);
                        var content = await reader.ReadToEndAsync();

                        OrgServiceMock.Invocations.Add(new ApiInvocation()
                        {
                            Method = new HttpMethod(context.Request.Method),
                            Body = content,
                            Path = context.Request.Path,
                            Query = context.Request.QueryString,
                            Headers = context.Request.Headers.ToDictionary(k => k.Key, k => k.Value.ToString())
                        });
                    }
                }

                context.Request.Body.Seek(0, SeekOrigin.Begin);

                await _next(context);
            }
            finally
            {
                OrgServiceMock.semaphore.Release();
            }
        }
    }

    public class RequestMockMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestMockMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            // Check if we want to intercept this
            var interceptor = OrgRequestMocker.Current.GetInterceptor(context.Request);
            if (interceptor is not null)
            {
                var content = await context.Request.ReadRequestBodyAsync();
                await interceptor.Processor(content, context);
            }
            else
            {
                await _next(context);
            }
        }
    }
}
