using System.Reflection;
using FluentValidation;
using FluentValidation.AspNetCore;
using Fusion.AspNetCore.OData;
using Fusion.Summary.Api.Database;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("/app/secrets/appsettings.secrets.yaml", optional: true)
    .AddJsonFile("/app/static/config/env.json", optional: true, reloadOnChange: true);

var azureAdClientId = builder.Configuration["AzureAd:ClientId"];
var azureAdClientSecret = builder.Configuration["AzureAd:ClientSecret"];
var fusionEnvironment = builder.Configuration["FUSION_ENVIRONMENT"];
var databaseConnectionString = builder.Configuration.GetConnectionString(nameof(SummaryDbContext));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks()
    .AddCheck("liveness", () => HealthCheckResult.Healthy())
    .AddCheck("db", () => HealthCheckResult.Healthy(), tags: ["ready"]);
// TODO: Add a real health check, when database is added
// .AddDbContextCheck<DatabaseContext>("db", tags: new[] { "ready" });

builder.Services.AddSwaggerGen(c =>
{
    c.OperationFilter<ODataFilterParamSwaggerFilter>();
    c.OperationFilter<ODataExpandParamSwaggerFilter>();
    c.OperationFilter<ODataOrderByParamSwaggerFilter>();
    c.OperationFilter<ODataTopParamSwaggerFilter>();
    c.OperationFilter<ODataSkipParamSwaggerFilter>();
    c.OperationFilter<ODataSearchParamSwaggerFilter>();
    c.DocumentFilter<ODataQueryParamSwaggerFilter>();
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Audience = builder.Configuration["AzureAd:ClientId"];
        options.Authority = "https://login.microsoftonline.com/3aa4a235-b6e2-48d5-9195-7fcf05b459b0";
        options.SaveToken = true;
    });

builder.Services.AddFusionIntegration(f =>
{
    f.AddFusionAuthorization();
    f.UseServiceInformation("Fusion.Summary.Api", "Dev");
    f.UseDefaultEndpointResolver(fusionEnvironment ?? "ci");
    f.UseDefaultTokenProvider(opts =>
    {
        opts.ClientId = azureAdClientId ?? throw new InvalidOperationException("Missing AzureAd:ClientId");
        opts.ClientSecret = azureAdClientSecret;
    });
});

builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddDbContext<SummaryDbContext>(options => options.UseSqlServer(databaseConnectionString));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
builder.Services.AddFluentValidationAutoValidation().AddValidatorsFromAssembly(typeof(Program).Assembly);

var app = builder.Build();
app.UseCors(opts => opts
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader()
    .WithExposedHeaders("Allow", "x-fusion-retriable"));
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseHttpsRedirection();
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
///     For testing
/// </summary>
public partial class Program
{
}