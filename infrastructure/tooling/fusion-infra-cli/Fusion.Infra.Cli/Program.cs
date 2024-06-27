// See https://aka.ms/new-console-template for more information
using Fusion.Infra.Cli;
using Fusion.Infra.Cli.Commands;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;



var app = Setup.CreateCliApp();

try
{
    await app.ExecuteAsync(args);
}
catch (Exception ex)
{
    Console.WriteLine("Error executing command...");
    Console.WriteLine("Command: " + Regex.Replace(Environment.CommandLine, @"-t [^.]+\.[^.]+\.[^\s]+", "-t [hidden token]"));
    Console.WriteLine(ex.Message);
    Console.WriteLine(ex.ToString());

    // Ensure -1 is returned to caller
    System.Environment.Exit(-1);
}


public static class Setup
{
    /// <summary>
    /// Setup the CLI application. Can use setup for overriding or adding additional services (testing).
    /// </summary>
    public static CommandLineApplication<MainCommand> CreateCliApp(Action<IServiceCollection>? setup = null)
    {
        var services = new ServiceCollection()
            .AddHttpClient()
            .AddNamedHttpClients()
            .AddSingleton<IFileLoader, DefaultFileLoader>()
            .AddSingleton<ITokenProvider, DefaultTokenProvider>()
            .AddSingleton(PhysicalConsole.Singleton);

        setup?.Invoke(services);

        var provider = services.BuildServiceProvider();


        var app = new CommandLineApplication<MainCommand>
        {
            Name = "finf",
            Description = "Fusion infrastructure tools",
        };

        app.Conventions
            .UseDefaultConventions()
            .UseConstructorInjection(provider)
            .UseDefaultHelpOption();

        return app;
    }

    public static IServiceCollection AddNamedHttpClients(this IServiceCollection services)
    {
        services.AddTransient<GraphTokenDelegateHandler>();
        services.AddHttpClient(Constants.GraphClientName, client =>
        {
            client.BaseAddress = new Uri("https://graph.microsoft.com/v1.0");
        })
            .AddHttpMessageHandler<GraphTokenDelegateHandler>();

        return services;
    }

}
