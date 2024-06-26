// See https://aka.ms/new-console-template for more information
using Fusion.Infra.Cli;
using Fusion.Infra.Cli.Commands;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.Conventions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;



var app = Setup.CreateCliApp();

try
{
    await app.ExecuteAsync(args);
}
catch (Exception ex)
{
    Console.WriteLine("Error executing command...");
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
            //.AddNamedHttpClients()
            //.AddSingleton(config)
            //.AddSingleton<IConfiguration>(config)
            //.AddSingleton(pca)
            //.AddSingleton<ITokenProvider, TokenProvider>()
            //.AddSingleton<GitHubCredentialCache>()
            .AddSingleton(PhysicalConsole.Singleton)
            //.AddSingleton<IUserAccessor, UserAccessor>()
            //.AddSingleton<EntityCache>()
            //.AddSingleton<Protector>()
            ;

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

public interface IFileLoader
{
    bool Exists(string path);
    string GetContent(string path);
}

public class DefaultFileLoader : IFileLoader
{
    public bool Exists(string path)
    {
        return File.Exists(path);
    }

    public string GetContent(string path)
    {
        return File.ReadAllText(path);
    }
}