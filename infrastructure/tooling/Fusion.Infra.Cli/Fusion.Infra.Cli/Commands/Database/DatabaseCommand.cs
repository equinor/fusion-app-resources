using McMaster.Extensions.CommandLineUtils;

namespace Fusion.Infra.Cli.Commands.Database
{
    [Command("database", Description = "Manage fusion database")]
    [Subcommand(
        typeof(ProvisionDatabaseCommand)
    )]
    internal class DatabaseCommand : CommandBase
    {
        public override Task OnExecuteAsync(CommandLineApplication app)
        {
            app.ShowHelp();
            return Task.CompletedTask;
        }
    }
}
