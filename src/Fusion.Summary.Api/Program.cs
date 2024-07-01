using Fusion.AspNetCore.Mvc.Versioning;
using Fusion.Resources.Api.Middleware;
using Fusion.Summary.Api.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("/app/secrets/appsettings.secrets.yaml", optional: true)
    .AddJsonFile("/app/static/config/env.json", optional: true, reloadOnChange: true);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration)
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();


builder.Services.AddApiVersioning(s =>
{
    s.ReportApiVersions = true;
    s.AssumeDefaultVersionWhenUnspecified = true;
    s.DefaultApiVersion = new ApiVersion(1, 0);
    s.ApiVersionReader = new HeaderOrQueryVersionReader("api-version");
});

builder.Services.AddSwagger(builder.Configuration);

builder.Services.AddFusionIntegration(f =>
{
    f.UseServiceInformation("Fusion.Summary.Api", "Dev");
    f.UseDefaultEndpointResolver(builder.Configuration["FUSION_ENVIRONMENT"] ?? "ci");
    f.UseDefaultTokenProvider(opts =>
    {
        opts.ClientId = builder.Configuration["AzureAd:ClientId"];
        opts.ClientSecret = builder.Configuration["AzureAd:ClientSecret"];
    });
});

builder.Services.AddApplicationInsightsTelemetry();

var app = builder.Build();
app.UseCors(opts => opts
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader()
    .WithExposedHeaders("Allow", "x-fusion-retriable"));

//app.UseMiddleware<RequestResponseLoggingMiddleware>();
app.UseMiddleware<TraceMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();

app.UseSummaryApiSwagger();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers().RequireAuthorization();
app.MapHealthChecks("/_health/liveness");
app.MapHealthChecks("/_health/readiness");
app.Run();
