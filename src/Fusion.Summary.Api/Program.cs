using Fusion.AspNetCore.OData;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("/app/secrets/appsettings.secrets.yaml", optional: true)
    .AddJsonFile("/app/static/config/env.json", optional: true, reloadOnChange: true);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();
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
    .AddMicrosoftIdentityWebApi(builder.Configuration)
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

builder.Services.AddFusionIntegration(f =>
{
    f.UseServiceInformation("Fusion.Summary.Api", "Dev");
    f.UseDefaultEndpointResolver(builder.Configuration["FUSION_ENVIRONMENT"] ?? "ci");
    f.UseDefaultTokenProvider(opts =>
    {
        opts.ClientId = builder.Configuration["AzureAd:ClientId"]!;
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
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers().RequireAuthorization();
app.MapHealthChecks("/_health/liveness");
app.MapHealthChecks("/_health/readiness");
app.Run();