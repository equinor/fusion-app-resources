namespace Microsoft.Extensions.DependencyInjection;

public static class StartupExtensions
{
    public static IServiceCollection AddSwagger(this IServiceCollection services, IConfiguration config)
    {
        services.AddSwagger(config, "Fusion Summary API", swagger => { });

        return services;
    }

    public static IApplicationBuilder UseSummaryApiSwagger(this IApplicationBuilder app)
    {
        app.UseFusionSwagger();

        return app;
    }
}