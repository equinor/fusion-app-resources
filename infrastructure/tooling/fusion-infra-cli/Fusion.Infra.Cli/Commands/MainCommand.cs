using Fusion.Infra.Cli.Commands.Database;
using McMaster.Extensions.CommandLineUtils;

namespace Fusion.Infra.Cli.Commands
{
    [Command("finf")]
    [Subcommand(
        typeof(DatabaseCommand),
        typeof(UnpackTokenCommand)
    )]
    public class MainCommand : CommandBase
    {
        public override Task OnExecuteAsync(CommandLineApplication app)
        {
            app.ShowHelp();

            return Task.CompletedTask;
        }
    }
}
