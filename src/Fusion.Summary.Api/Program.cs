using Fusion.AspNetCore.Mvc.Versioning;
using Fusion.Resources.Api.Middleware;
using Fusion.Summary.Api.Middleware;
using System.Reflection;
using Fusion.Summary.Api.Database;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("/app/secrets/appsettings.secrets.yaml", optional: true)
    .AddJsonFile("/app/static/config/env.json", optional: true, reloadOnChange: true);

var azureAdClientId = builder.Configuration["AzureAd:ClientId"];
var azureAdClientSecret = builder.Configuration["AzureAd:ClientSecret"];
var fusionEnvironment = builder.Configuration["FUSION_ENVIRONMENT"];
var databaseConnectionString = builder.Configuration.GetConnectionString("DatabaseContext");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();
builder.Services.AddSwaggerGen();

var foo = builder.Configuration["AzureAd:ClientId"];

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
    f.UseServiceInformation("Fusion.Summary.Api", "Dev");
    f.UseDefaultEndpointResolver(fusionEnvironment ?? "ci");
    f.UseDefaultTokenProvider(opts =>
    {
        opts.ClientId = azureAdClientId ?? throw new InvalidOperationException("Missing AzureAd:ClientId");
        opts.ClientSecret = azureAdClientSecret;
    });
});

builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddDbContext<DatabaseContext>(options => options.UseSqlServer(databaseConnectionString));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

var app = builder.Build();
app.UseCors(opts => opts
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader()
    .WithExposedHeaders("Allow", "x-fusion-retriable"));

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
app.UseHealthChecks("/_health/ready", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("ready")
});

#endregion Health probes

app.Run();
