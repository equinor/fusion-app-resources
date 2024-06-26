// See https://aka.ms/new-console-template for more information
using Fusion.Infra.Cli.Commands;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;



var app = Setup.CreateCliApp();

try
{
    await app.ExecuteAsync(args);
}
catch (Exception ex)
{
    Console.WriteLine("Error executing command...");
    Console.WriteLine("Command: " + Environment.CommandLine);
    Console.WriteLine(ex.Message);
    Console.WriteLine(ex.ToString());
    
    // Ensure -1 is returned to caller
    throw;
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
            .AddSingleton<IFileLoader, DefaultFileLoader>()
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
}
