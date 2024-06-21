using Fusion.Summary.Api.Database;
using Fusion.Summary.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Web;
using System.Reflection;

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
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration)
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();
builder.Services.AddFusionIntegration(f =>
{
    f.UseServiceInformation("Fusion.Summary.Api", "Dev");
    f.UseDefaultEndpointResolver(fusionEnvironment ?? "ci");
    f.UseDefaultTokenProvider(opts =>
    {
        opts.ClientId = azureAdClientId;
        opts.ClientSecret = azureAdClientSecret;
    });
});
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddDbContext<DatabaseContext>(options => options.UseSqlServer(databaseConnectionString));
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

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
app.MapHealthChecks("/_health/liveness");
app.MapHealthChecks("/_health/readiness");
app.Run();
