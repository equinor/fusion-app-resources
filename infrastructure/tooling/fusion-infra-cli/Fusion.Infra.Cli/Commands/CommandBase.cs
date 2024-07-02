using McMaster.Extensions.CommandLineUtils;

namespace Fusion.Infra.Cli.Commands
{
    public abstract class CommandBase
    {
        public abstract Task OnExecuteAsync(CommandLineApplication app);
    }
}
