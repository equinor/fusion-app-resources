using McMaster.Extensions.CommandLineUtils;
using System.Windows.Input;

namespace Fusion.Infra.Cli.Commands
{
    public abstract class CommandBase
    {
        public abstract Task OnExecuteAsync(CommandLineApplication app);
    }
}
