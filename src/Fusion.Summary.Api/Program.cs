using System.Reflection;
using FluentValidation;
using Fusion.AspNetCore.Versioning;
using Fusion.Resources.Api.Middleware;
using Fusion.Summary.Api;
using Fusion.Summary.Api.Database;
using Fusion.Summary.Api.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;

var builder = WebApplication.CreateBuilder(args);

if (Environment.GetEnvironmentVariable("INTEGRATION_TEST_RUN") != "true")
{
    builder.Configuration
        .AddJsonFile("/app/secrets/appsettings.secrets.yaml", optional: true)
        .AddJsonFile("/app/config/appsettings.json", optional: true); // to be able to override settings by using a config map in kubernetes

    builder.AddKeyVault();
}

var azureAdClientId = builder.Configuration["AzureAd:ClientId"] ?? throw new InvalidOperationException("Missing AzureAd:ClientId");
var azureAdClientSecret = builder.Configuration["AzureAd:ClientSecret"];
var certThumbprint = builder.Configuration["Config:CertThumbprint"];
var environment = builder.Configuration["Environment"] ?? "Development";
var fusionEnvironment = builder.Configuration["FUSION_ENVIRONMENT"] ?? "ci";
var databaseConnectionString = builder.Configuration.GetConnectionString(nameof(SummaryDbContext))!;

builder.Services.AddFluentValidationAutoValidation(c =>
{
    c.EnablePathBindingSourceAutomaticValidation = true;
    c.EnableFormBindingSourceAutomaticValidation = true;
});
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks()
    .AddCheck("liveness", () => HealthCheckResult.Healthy())
    .AddDbContextCheck<SummaryDbContext>("db", tags: new[] { "ready" });

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Audience = builder.Configuration["AzureAd:ClientId"];
        options.Authority = "https://login.microsoftonline.com/3aa4a235-b6e2-48d5-9195-7fcf05b459b0";
        options.SaveToken = true;
    });


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
    f.AddFusionAuthorization();
    f.UseServiceInformation("Fusion.Summary.Api", environment);
    f.UseDefaultEndpointResolver(fusionEnvironment);
    f.UseDefaultTokenProvider(opts =>
    {
        opts.ClientId = azureAdClientId;
        opts.ClientSecret = azureAdClientSecret;
        opts.CertificateThumbprint = certThumbprint;
    });
});

builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddSqlDbContext<SummaryDbContext>(databaseConnectionString)
    .AddSqlTokenProvider<SqlTokenProvider>()
    .AddAccessTokenSupport();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

builder.Services.AddFluentValidationAutoValidation(c =>
{
    c.EnablePathBindingSourceAutomaticValidation = true;
    c.EnableFormBindingSourceAutomaticValidation = true;
});
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

var app = builder.Build();
app.UseCors(opts => opts
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader()
    .WithExposedHeaders("Allow", "x-fusion-retriable", "x-trace-id"));

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

//app.UseMiddleware<RequestResponseLoggingMiddleware>();
app.UseMiddleware<TraceMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();

app.UseSummaryApiSwagger();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers().RequireAuthorization();

#region Health probes

app.UseHealthChecks("/_health/liveness", new HealthCheckOptions
{
    Predicate = r => r.Name.Contains("liveness")
});
app.UseHealthChecks("/_health/readiness", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("ready")
});

#endregion Health probes

app.Run();

/// <summary>
///     For testing.
/// </summary>
public partial class Program
{
}